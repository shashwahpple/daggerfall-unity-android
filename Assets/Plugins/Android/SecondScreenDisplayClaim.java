package com.dfworkshop.daggerfallunityandroid;

import android.app.Activity;
import android.app.ActivityOptions;
import android.content.Context;
import android.content.Intent;
import android.hardware.display.DisplayManager;
import android.util.Log;
import android.view.Display;

/**
 * Entry point for claiming the AYN Thor's second display - see SecondScreenOwnerActivity and
 * UnityPresentationFocusPatcher for the two halves of the actual fix. Called once from C# when
 * SecondScreenManager sets up Display 2 (Assets/Android/Scripts/SecondScreen/SecondScreenManager.cs).
 */
public final class SecondScreenDisplayClaim
{
    private static final String TAG = "SecondScreenOwner";

    private SecondScreenDisplayClaim() {
    }

    public static void claim(final Activity activity) {
        if (activity == null) {
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                claimInternal(activity);
            }
        });
    }

    private static void claimInternal(Activity activity) {
        DisplayManager displayManager = (DisplayManager) activity.getSystemService(Context.DISPLAY_SERVICE);
        if (displayManager == null) {
            Log.e(TAG, "No DisplayManager available");
            return;
        }

        Display target = null;
        for (Display display : displayManager.getDisplays()) {
            if (display.getDisplayId() != Display.DEFAULT_DISPLAY) {
                target = display;
                break;
            }
        }

        if (target == null) {
            Log.e(TAG, "No secondary display found");
            return;
        }

        Intent intent = new Intent(activity, SecondScreenOwnerActivity.class);
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_MULTIPLE_TASK);

        ActivityOptions options = ActivityOptions.makeBasic();
        options.setLaunchDisplayId(target.getDisplayId());

        activity.startActivity(intent, options.toBundle());
        Log.i(TAG, "launched SecondScreenOwnerActivity on display id " + target.getDisplayId());

        UnityPresentationFocusPatcher.patchWithRetries(activity);
    }
}
