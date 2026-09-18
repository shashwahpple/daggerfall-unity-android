package com.dfworkshop.daggerfallunityandroid;

import android.app.Activity;
import android.graphics.Color;
import android.graphics.PixelFormat;
import android.os.Bundle;
import android.os.IBinder;
import android.util.Log;
import android.view.Gravity;
import android.view.View;
import android.view.WindowManager;
import android.widget.FrameLayout;

/**
 * Claims Display 2 as a real Activity/Task, launched via ActivityOptions.setLaunchDisplayId (see
 * SecondScreenDisplayClaim), so ActivityTaskManager attributes a real window/task to our own app
 * there instead of falling back to Android's SECONDARY_HOME resolution
 * (com.android.launcher3/.secondarydisplay.SecondaryDisplayLauncher, or any third-party app that
 * registers for that category) on tap. Fixes the AYN Thor controller-freeze-on-tap bug tracked at
 * https://github.com/shashwahpple/daggerfall-unity-android/issues/1.
 *
 * Claiming the Task alone is not sufficient: Android's InputDispatcher tracks a single systemwide
 * focused display, and a physical controller (unassociated with any specific display) sends its
 * key/motion events to whichever display last took focus - independent of ActivityTaskManager's
 * separate per-display "focused app" bookkeeping. Without the fix below, tapping Display 2 would
 * make it the focused display and starve Unity's own window (on Display 1) of controller input.
 *
 * Fix (ported from Josh-Daniels/OpenMW-DS commit 34eb5ce0, "Fixed focus issues for retroid
 * devices" - the AYN Thor community's own fix for this exact bug on this exact device):
 * - FLAG_NOT_FOCUSABLE from onCreate ANRs: an Activity that is the only window of its task cannot
 *   be non-focusable, because the display then has a focused app and no focusable window, and
 *   InputDispatcher waits 5s for one before declaring the app hung.
 * - So this window stays focusable until its first real onWindowFocusChanged(true) callback -
 *   satisfying that requirement, and firing automatically as part of normal Activity startup, well
 *   before the player has a chance to touch anything - then adds a 1x1 FLAG_NOT_TOUCHABLE (but
 *   still focusable) anchor window, and only once that anchor is confirmed in place does it add
 *   FLAG_NOT_FOCUSABLE to itself. The anchor keeps satisfying InputDispatcher's "this display
 *   needs a focusable window" requirement forever after, while being physically untappable, so no
 *   future tap on Display 2 can make it the focused display again - the controller stays with
 *   Unity's window from then on.
 * - Order is load-bearing: anchor first, flag second. If the anchor fails to attach, do NOT drop
 *   focusability - that regresses to the original bug (recoverable by tapping Display 1) rather
 *   than an ANR.
 *
 * Unity's own Android multi-display implementation additionally hosts Display 2's actual rendered
 * pixels as its own separate android.app.Presentation internally (confirmed via `dumpsys window
 * windows`: a `ty=PRESENTATION` window owned by this same process, at a higher window layer than
 * anything built here), and that Presentation is ordinarily focusable - so it, not this Activity's
 * own window, is what a tap actually lands on. See UnityPresentationFocusPatcher for the
 * corresponding fix to that separate window.
 */
public final class SecondScreenOwnerActivity extends Activity
{
    private static final String TAG = "SecondScreenOwner";

    private boolean focusabilityDropped;
    private View focusAnchorView;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        getWindow().addFlags(WindowManager.LayoutParams.FLAG_NOT_TOUCH_MODAL);

        // Never visible - Unity's own Display 2 rendering (see class doc) always covers it. This
        // window exists purely to claim the Task and, briefly, satisfy InputDispatcher's focusable-
        // window requirement until the anchor takes over that job.
        FrameLayout root = new FrameLayout(this);
        root.setBackgroundColor(Color.BLACK);
        setContentView(root);
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (hasFocus) {
            dropFocusabilityOnceFocused();
        }
    }

    private void dropFocusabilityOnceFocused() {
        if (focusabilityDropped) {
            return;
        }
        focusabilityDropped = true;

        getWindow().getDecorView().post(new Runnable() {
            @Override
            public void run() {
                if (!addFocusAnchor()) {
                    focusabilityDropped = false;
                    Log.e(TAG, "no focus anchor, staying focusable rather than risking an ANR");
                    return;
                }

                getWindow().addFlags(WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE);
                Log.i(TAG, "dropped focusability, anchor in place on display "
                        + getWindowManager().getDefaultDisplay().getDisplayId());
            }
        });
    }

    private boolean addFocusAnchor() {
        if (focusAnchorView != null) {
            return true;
        }

        IBinder token = getWindow().getDecorView().getWindowToken();
        if (token == null) {
            Log.w(TAG, "focus anchor: no window token yet");
            return false;
        }

        WindowManager.LayoutParams lp = new WindowManager.LayoutParams(
                1, 1,
                WindowManager.LayoutParams.TYPE_APPLICATION_PANEL,
                WindowManager.LayoutParams.FLAG_NOT_TOUCHABLE | WindowManager.LayoutParams.FLAG_NOT_TOUCH_MODAL,
                PixelFormat.TRANSPARENT);
        lp.token = token;
        lp.gravity = Gravity.TOP | Gravity.START;

        View view = new View(this);
        try {
            getWindowManager().addView(view, lp);
            focusAnchorView = view;
            Log.i(TAG, "focus anchor added");
            return true;
        } catch (Exception e) {
            Log.e(TAG, "focus anchor addView failed", e);
            return false;
        }
    }

    private void removeFocusAnchor() {
        if (focusAnchorView == null) {
            return;
        }
        try {
            getWindowManager().removeView(focusAnchorView);
        } catch (Exception ignored) {
        }
        focusAnchorView = null;
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        removeFocusAnchor();
    }
}
