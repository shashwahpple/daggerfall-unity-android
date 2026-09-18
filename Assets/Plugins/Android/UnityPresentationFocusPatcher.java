package com.dfworkshop.daggerfallunityandroid;

import android.app.Activity;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.view.View;
import android.view.WindowManager;

import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.util.List;

/**
 * Unity's own Android multi-display implementation hosts Display 2's rendering surface as a real
 * android.app.Presentation internally (confirmed on-device via `dumpsys window windows`: a
 * `ty=PRESENTATION` window owned by our own process, at a HIGHER window layer than anything
 * SecondScreenOwnerActivity builds). It is ordinarily focusable, and since it visually and
 * touch-hit-test-wise sits above everything else on that display, it - not
 * SecondScreenOwnerActivity's own (already non-focusable, anchored) window - is what actually
 * receives the tap. No public Unity API exposes this Presentation to make it non-focusable
 * directly.
 *
 * This reaches it via reflection into WindowManagerGlobal's internal per-process window list
 * (mViews/mParams - hidden API, may break across Android versions) to find the Presentation-typed
 * window and add FLAG_NOT_FOCUSABLE to it directly, the same way SecondScreenOwnerActivity's own
 * anchor pattern does for its own window. Safe even if Unity creates its Presentation on a delay:
 * retries on a short timer, and SecondScreenOwnerActivity's own anchor already guarantees Display 2
 * has a focusable window throughout, so patching this one late (or never, if reflection breaks)
 * cannot cause the ANR that a lone non-focusable window would.
 */
final class UnityPresentationFocusPatcher
{
    private static final String TAG = "SecondScreenOwner";
    private static final int TYPE_PRESENTATION = 2037;
    private static final int MAX_ATTEMPTS = 20;
    private static final long RETRY_DELAY_MS = 250;

    private UnityPresentationFocusPatcher() {
    }

    static void patchWithRetries(final Activity activity) {
        final Handler handler = new Handler(Looper.getMainLooper());
        attempt(activity, handler, 0);
    }

    private static void attempt(final Activity activity, final Handler handler, final int attemptNumber) {
        if (tryPatchOnce(activity)) {
            return;
        }
        if (attemptNumber >= MAX_ATTEMPTS) {
            Log.e(TAG, "gave up looking for Unity's Presentation window after " + MAX_ATTEMPTS + " attempts");
            return;
        }
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                attempt(activity, handler, attemptNumber + 1);
            }
        }, RETRY_DELAY_MS);
    }

    private static boolean tryPatchOnce(Activity activity) {
        try {
            Class<?> wmgClass = Class.forName("android.view.WindowManagerGlobal");
            Method getInstance = wmgClass.getMethod("getInstance");
            Object wmg = getInstance.invoke(null);

            Field viewsField = wmgClass.getDeclaredField("mViews");
            viewsField.setAccessible(true);
            @SuppressWarnings("unchecked")
            List<View> views = (List<View>) viewsField.get(wmg);

            Field paramsField = wmgClass.getDeclaredField("mParams");
            paramsField.setAccessible(true);
            @SuppressWarnings("unchecked")
            List<WindowManager.LayoutParams> params = (List<WindowManager.LayoutParams>) paramsField.get(wmg);

            if (views == null || params == null) {
                return false;
            }

            for (int i = 0; i < views.size() && i < params.size(); i++) {
                WindowManager.LayoutParams lp = params.get(i);
                if (lp == null || lp.type != TYPE_PRESENTATION) {
                    continue;
                }
                if ((lp.flags & WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE) != 0) {
                    return true;
                }

                View view = views.get(i);
                WindowManager.LayoutParams patched = new WindowManager.LayoutParams();
                patched.copyFrom(lp);
                patched.flags |= WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE;

                activity.getWindowManager().updateViewLayout(view, patched);
                Log.i(TAG, "patched Unity's Presentation window to FLAG_NOT_FOCUSABLE");
                return true;
            }

            return false;
        } catch (Exception e) {
            Log.e(TAG, "reflection patch failed", e);
            return false;
        }
    }
}
