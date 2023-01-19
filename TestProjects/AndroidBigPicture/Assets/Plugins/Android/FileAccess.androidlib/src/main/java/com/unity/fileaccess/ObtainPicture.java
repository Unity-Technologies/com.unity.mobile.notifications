package com.unity.fileaccess;

import java.io.*;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;

public class ObtainPicture implements FileChooser.Response {
    Context mContext;
    boolean mFinished = false;
    String mDestPath;

    public ObtainPicture(Context context, String path) {
        mContext = context;
        mDestPath = path;
        FileChooser.response = this;
        Intent intent = new Intent(context, FileChooser.class);
        context.startActivity(intent);
    }

    public void result(InputStream content) {
        try {
            BufferedInputStream in = new BufferedInputStream(content);
            BufferedOutputStream out = new BufferedOutputStream(new FileOutputStream(mDestPath));
            byte[] buffer = new byte[1024 * 32];
            int count;
            while ((count = in.read(buffer)) > 0) {
                out.write(buffer, 0, count);
                if (count < buffer.length)
                    break;
            }
            in.close();
            out.close();
        } catch (FileNotFoundException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }

        mFinished = true;
    }

    public boolean isFinished() {
        return mFinished;
    }

    public String getUri() {
        return getUri(mContext, mDestPath);
    }

    public static String getUri(Context context, String path) {
        File file = new File(path);
        Uri uri = ImageProvider.getUriForFile(context, "com.unity.fileaccess", file);
        return uri.toString();
    }
}
