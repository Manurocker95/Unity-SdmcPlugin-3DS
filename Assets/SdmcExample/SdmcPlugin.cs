using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SdmcPlugin : MonoBehaviour {
    public enum SdmcResult
    {
        SDMC_SUCCESS = 0,
        SDMC_INVALID_ARGUMENT,
        SDMC_NOT_MOUNTED,
        SDMC_OPEN_FAILED,
        SDMC_READ_FAILED,
        SDMC_WRITE_FAILED,
        SDMC_PARTIAL_WRITE,
        SDMC_CREATE_DIRECTORY_FAILED,
        SDMC_MOUNT_FAILED,
        SDMC_STREAM_CLOSED,
        SDMC_FLUSH_FAILED,
        SDMC_UNMOUNT_FAILED,
        SDMC_FILE_NOT_FOUND,
        SDMC_INVALID_PATH,
        SDMC_DELETE_FAILED,
    }

    private static string _basePath;

    /// <summary>Precedes all file paths when calling SdmcPlugin functions</summary>
    public static string BasePath { 
        get 
        {
            if (string.IsNullOrEmpty(_basePath))
            {
                _basePath = "sdmc:/Unity/" + Application.companyName.Replace(" ", "_") + "/" + Application.productName.Replace(" ", "_") + "/";
            }
            return _basePath; 
        } 
    }

    /// <summary>SDMC is mounted automatically, no need to use this</summary>
    [DllImport("__Internal")]
    public static extern SdmcResult SdmcMount();

    /// <summary>SDMC is not unmounted automatically, might be wise to use this</summary>
    [DllImport("__Internal")]
    public static extern SdmcResult SdmcUnmount();


    [DllImport("__Internal")]
    private static extern SdmcResult SdmcFileExists(string path);

    [DllImport("__Internal")]
    private static extern SdmcResult SdmcDirectoryExists(string path);


    [DllImport("__Internal")]
    public static extern SdmcResult SdmcOpenWriteStream(string path, out IntPtr stream);

    [DllImport("__Internal")]
    public static extern SdmcResult SdmcWriteStream(IntPtr stream, byte[] data, int size);

    [DllImport("__Internal")]
    public static extern SdmcResult SdmcCloseWriteStream( IntPtr stream);


    [DllImport("__Internal")]
    public static extern SdmcResult SdmcOpenReadStream(string path, out IntPtr stream);

    [DllImport("__Internal")]
    public static extern SdmcResult SdmcReadStream(IntPtr stream, byte[] buffer, int size, out int bytesRead);

    [DllImport("__Internal")]
    public static extern SdmcResult SdmcCloseReadStream(IntPtr stream);


    [DllImport("__Internal")]
    private static extern SdmcResult SdmcFileDelete(string path);

    [DllImport("__Internal")]
    private static extern SdmcResult SdmcDirectoryDelete(string path);


    [DllImport("__Internal")]
    private static extern IntPtr SdmcGetErrorString(int result);

    [DllImport("__Internal")]
    public static extern SdmcResult SdmcSeekReadStream(IntPtr stream, int offset, int origin);

    public static string GetErrorString(SdmcResult result)
    {
        return Marshal.PtrToStringAnsi(SdmcGetErrorString((int)result));
    }

    /// <summary>Read data from relative path. BasePath is added automatically</summary>
    public static SdmcResult FileExists(string path)
    {
        path = Path.Combine(BasePath, path);
        return SdmcFileExists(path);
    }

    /// <summary>Read data from relative path. BasePath is added automatically</summary>
    public static byte[] ReadFile(string path)
    {
        path = Path.Combine(BasePath, path);
        using (var reader = new SdmcReadableStream(path))
        {
            return reader.ReadAll();
        }
    }

    /// <summary>Write data to relative path. BasePath is added automatically</summary>
    public static void WriteFile(string path, byte[] data)
    {
        path = Path.Combine(BasePath, path);
        using (var writer = new SdmcWritableStream(path))
        {
            writer.Write(data, 0, data.Length);
        }
    }

    /// <summary>Write object to relative path. BasePath is added automatically</summary>
    public static void WriteObject(string path, object obj)
    {
        path = Path.Combine(BasePath, path);
        BinaryFormatter bf = new BinaryFormatter();

        using (var writer = new SdmcWritableStream(path))
        {
            bf.Serialize(writer, obj);
        }
    }

    /// <summary>Read object of type T from relative path. BasePath is added automatically</summary>
    public static T ReadObject<T>(string path) where T : class
    {
        path = Path.Combine(BasePath, path);
        BinaryFormatter bf = new BinaryFormatter();
        using (var reader = new SdmcReadableStream(path))
        {
            object obj = bf.Deserialize(reader);

            return obj as T;
        }
    }

    /// <summary>Delete file at relative path. BasePath is added automatically</summary>
    public static SdmcResult DeleteFile(string path)
    {
        path = Path.Combine(BasePath, path);
        return SdmcFileDelete(path);
    }

    /// <summary>Delete directory at relative path. BasePath is added automatically</summary>
    public static SdmcResult DeleteDirectory(string path)
    {
        path = Path.Combine(BasePath, path);
        return SdmcDirectoryDelete(path);
    }
}
