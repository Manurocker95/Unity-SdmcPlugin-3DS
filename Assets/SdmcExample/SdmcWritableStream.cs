using System;
using System.IO;

public sealed class SdmcWritableStream : Stream
{
    private IntPtr handle;
    private bool disposed;

    public SdmcWritableStream(string path)
    {
        var result = SdmcPlugin.SdmcOpenWriteStream(path, out handle);

        if (result != SdmcPlugin.SdmcResult.SDMC_SUCCESS)
            throw new Exception(SdmcPlugin.GetErrorString(result));
    }

    public override bool CanRead { get { return false; } }
    public override bool CanSeek { get { return false; } }
    public override bool CanWrite { get { return !disposed; } }
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
        throw new NotSupportedException();
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
        if (disposed)
            throw new Exception("Stream disposed");

        if (buffer == null)
            throw new Exception("Buffer is null");

        if (offset == 0 && count == buffer.Length)
        {
            var result = SdmcPlugin.SdmcWriteStream(handle, buffer, count);

            if (result != SdmcPlugin.SdmcResult.SDMC_SUCCESS)
                throw new Exception(SdmcPlugin.GetErrorString(result));

            return;
        }

        byte[] temp = new byte[count];
        Buffer.BlockCopy(buffer, offset, temp, 0, count);

        var copyResult = SdmcPlugin.SdmcWriteStream(handle, temp, count);

        if (copyResult != SdmcPlugin.SdmcResult.SDMC_SUCCESS)
            throw new Exception(SdmcPlugin.GetErrorString(copyResult));
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (handle != IntPtr.Zero)
            {
                SdmcPlugin.SdmcCloseWriteStream(handle);
                handle = IntPtr.Zero;
            }

            disposed = true;
        }

        base.Dispose(disposing);
    }
}