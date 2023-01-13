package com.unity.fileaccess;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import java.io.FileNotFoundException;
import java.io.InputStream;

public class FileChooser extends Activity {
    private static final int FILE_CHOICE = 123456;

    public interface Response {
        void result(InputStream content);
    }

    public static Response response;

    @Override
    protected void onCreate(Bundle savedInstance) {
        super.onCreate(savedInstance);

        Intent intent = new Intent();
        intent.setType("image/*");
        intent.setAction(Intent.ACTION_GET_CONTENT);
        startActivityForResult(intent, FILE_CHOICE);
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode != FILE_CHOICE)
            return;
        if (resultCode != RESULT_OK) {
            response.result(null);
            finish();
            return;
        }

        try {
            response.result(getContentResolver().openInputStream(data.getData()));
        } catch (FileNotFoundException e) {
            e.printStackTrace();
            response.result(null);
        }

        finish();
    }
}
