using System;
using System.IO;

public sealed class SdmcReadableStream : Stream
{
    private IntPtr handle;
    private bool disposed;

    public SdmcReadableStream(string path)
    {
        var result = SdmcPlugin.SdmcOpenReadStream(path, out handle);

        if (result != SdmcPlugin.SdmcResult.SDMC_SUCCESS)
            throw new Exception(SdmcPlugin.GetErrorString(result));
    }

    public override bool CanRead { get { return !disposed; } }
    public override bool CanSeek { get { return false; } }
    public override bool CanWrite { get { return false; } }
    public override long Length { get { throw new NotSupportedException(); } }
    public override long Position
    {
        get { throw new NotSupportedException(); }
        set { throw new NotSupportedException(); }
    }
    
    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (disposed)
            throw new Exception("Stream disposed");

        if (buffer == null)
            throw new Exception("Buffer is null");

        if (offset != 0 || count < 0 || count > buffer.Length)
            throw new ArgumentOutOfRangeException();

        int bytesRead;

        var result = SdmcPlugin.SdmcReadStream(handle, buffer, count, out bytesRead);

        if (result != SdmcPlugin.SdmcResult.SDMC_SUCCESS)
            throw new Exception(SdmcPlugin.GetErrorString(result));

        return bytesRead;
    }

    public byte[] ReadAll()
    {
        using (var memory = new MemoryStream())
        {
            byte[] buffer = new byte[4096];

            while (true)
            {
                int bytesRead = Read(buffer, 0, buffer.Length);

                if (bytesRead < 0)
                    throw new Exception("Read failed");

                if (bytesRead == 0)
                    break;

                memory.Write(buffer, 0, bytesRead);
            }

            return memory.ToArray();
        }
    }

    public string ReadAllText()
    {
        return System.Text.Encoding.UTF8.GetString(ReadAll());
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (handle != IntPtr.Zero)
            {
                var result = SdmcPlugin.SdmcCloseReadStream(handle);
                if (result != SdmcPlugin.SdmcResult.SDMC_SUCCESS)
                {
                    throw new Exception(SdmcPlugin.GetErrorString(result));
                }
                handle = IntPtr.Zero;
            }

            disposed = true;
        }

        base.Dispose(disposing);
    }
}