package com.unity.fileaccess;

import androidx.core.content.FileProvider;
import com.unity.fileaccess.R;

public class ImageProvider extends FileProvider {
    public ImageProvider() {
        super(R.xml.file_paths);
    }
}
