#include <stdio.h>
#include <string.h>
#include <nn/fs.h>

extern "C"
{
	enum SdmcResult
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
	};

	struct SdmcWritableStream
	{
		nn::fs::FileOutputStream file;
		bool isOpen;
	};

	struct SdmcReadableStream
	{
		nn::fs::FileInputStream file;
		bool isOpen;
	};

	bool sdCardMounted = false;

	SdmcResult SdmcMount() {
		if (sdCardMounted) {
			return SDMC_SUCCESS;
		}

		NN_LOG("Mounting SDMC\n");
		nn::Result result = nn::fs::MountSdmc();
		if (result.IsFailure())
		{
			return SDMC_MOUNT_FAILED;
		}

		sdCardMounted = true;
		return SDMC_SUCCESS;
	}

	SdmcResult SdmcUnmount() {
		if (!sdCardMounted) {
			return SDMC_SUCCESS;
		}

		NN_LOG("Unmounting SDMC\n");
		nn::Result result = nn::fs::Unmount("sdmc:");
		if (!result.IsSuccess()) {
			return SDMC_UNMOUNT_FAILED;
		}

		sdCardMounted = false;
		return SDMC_SUCCESS;
	}

	SdmcResult SdmcFileExists(const char* path)
	{
		if (SdmcMount() != SDMC_SUCCESS)
		{
			return SDMC_MOUNT_FAILED;
		}

		if (!path)
			return SDMC_INVALID_ARGUMENT;

		nn::fs::FileInputStream file;
		nn::Result result = file.TryInitialize(path);

		if (result.IsFailure())
			return SDMC_FILE_NOT_FOUND;

		file.Finalize();
		return SDMC_SUCCESS;
	}

	SdmcResult SdmcDirectoryExists(const char* path)
	{
		if (SdmcMount() != SDMC_SUCCESS)
		{
			return SDMC_MOUNT_FAILED;
		}

		if (!path)
			return SDMC_INVALID_ARGUMENT;

		nn::fs::Directory dir;
		nn::Result result = dir.TryInitialize(path);

		if (result.IsFailure())
			return SDMC_FILE_NOT_FOUND;

		dir.Finalize();
		return SDMC_SUCCESS;
	}

	static bool GetDirectoryPath(const char* filePath, char* out, size_t outSize)
	{
		const char* lastSlash = strrchr(filePath, '/');
		if (!lastSlash)
			return false;

		size_t len = lastSlash - filePath + 1;

		if (len >= outSize)
			return false;

		memcpy(out, filePath, len);
		out[len] = '\0';

		return true;
	}

	SdmcResult SdmcCreateDirectories(const char* filePath)
	{
		if (!filePath)
			return SDMC_INVALID_ARGUMENT;

		if (strncmp(filePath, "sdmc:/", 6) != 0)
			return SDMC_INVALID_ARGUMENT;

		char dir[512];

		if (!GetDirectoryPath(filePath, dir, sizeof(dir)))
			return SDMC_INVALID_PATH;

		if (SdmcDirectoryExists(dir) == SDMC_SUCCESS)
			return SDMC_SUCCESS;

		char temp[512];
		strcpy(temp, dir);

		size_t len = strlen(temp);

		for (size_t i = 0; i < len; i++)
		{
			if (temp[i] == '/' && i > 5)
			{
				temp[i] = '\0';

				nn::Result res = nn::fs::TryCreateDirectory(temp);

				if (res.IsFailure() && !nn::fs::ResultAlreadyExists::Includes(res))
				{
					return SDMC_CREATE_DIRECTORY_FAILED;
				}

				temp[i] = '/';
			}
		}

		return SDMC_SUCCESS;
	}

	SdmcResult SdmcOpenWriteStream(const char* path, SdmcWritableStream** outStream)
	{
		SdmcResult result = SdmcMount();
		if (result != SDMC_SUCCESS)
			return result;

		if (!path)
			return SDMC_INVALID_ARGUMENT;

		result = SdmcCreateDirectories(path);
		if (result != SDMC_SUCCESS)
			return result;

		SdmcWritableStream* stream = new SdmcWritableStream();

		nn::Result streamResult = stream->file.TryInitialize(path, true);

		if (streamResult.IsFailure())
		{
			delete stream;
			return SDMC_OPEN_FAILED;
		}

		stream->isOpen = true;
		*outStream = stream;
		return SDMC_SUCCESS;
	}

	SdmcResult SdmcWriteStream(SdmcWritableStream* stream, const void* data, size_t size)
	{
		if (!stream || !data)
			return SDMC_INVALID_ARGUMENT;

		if (!stream->isOpen)
			return SDMC_STREAM_CLOSED;

		s32 written = 0;

		nn::Result result = stream->file.TryWrite(&written, data, size, false);

		if (result.IsFailure())
			return SDMC_WRITE_FAILED;

		return ((size_t)written == size) ? SDMC_SUCCESS : SDMC_PARTIAL_WRITE;
	}

	SdmcResult SdmcCloseWriteStream(SdmcWritableStream* stream)
	{
		if (!stream)
			return SDMC_INVALID_ARGUMENT;

		if (stream->isOpen)
		{
			stream->isOpen = false;

			nn::Result result = stream->file.TryFlush();

			if (result.IsFailure()) {
				delete stream;
				return SDMC_FLUSH_FAILED;
			}

			stream->file.Finalize();
		}

		delete stream;
		return SDMC_SUCCESS;
	}

	SdmcResult SdmcOpenReadStream(const char* path, SdmcReadableStream** outStream)
	{
		SdmcResult result = SdmcMount();
		if (result != SDMC_SUCCESS)
			return result;

		if (!path)
			return SDMC_INVALID_ARGUMENT;

		SdmcReadableStream* stream = new SdmcReadableStream();

		nn::Result streamResult = stream->file.TryInitialize(path);

		if (streamResult.IsFailure())
		{
			delete stream;
			return SDMC_OPEN_FAILED;
		}

		stream->isOpen = true;
		*outStream = stream;
		return SDMC_SUCCESS;
	}

	SdmcResult SdmcReadStream(SdmcReadableStream* stream, void* buffer, size_t bufferSize, int* outRead)
	{
		if (!stream || !stream->isOpen || !buffer || !outRead)
			return SDMC_INVALID_ARGUMENT;

		s32 read = 0;

		nn::Result result = stream->file.TryRead(&read, buffer, bufferSize);

		if (result.IsFailure())
			return SDMC_READ_FAILED;

		*outRead = read;
		return SDMC_SUCCESS;
	}

	SdmcResult SdmcCloseReadStream(SdmcReadableStream* stream)
	{
		if (!stream)
			return SDMC_INVALID_ARGUMENT;

		if (stream->isOpen)
		{
			stream->file.Finalize();
			stream->isOpen = false;
		}

		delete stream;
		return SDMC_SUCCESS;
	}

	SdmcResult SdmcFileDelete(const char* path) {
		SdmcResult result = SdmcFileExists(path);
		if (result != SDMC_SUCCESS) {
			return SDMC_SUCCESS;
		}

		nn::Result deleteResult = nn::fs::TryDeleteFile(path);
		if (deleteResult.IsFailure()) {
			return SDMC_DELETE_FAILED;
		}

		return SDMC_SUCCESS;
	}

	SdmcResult SdmcDirectoryDelete(const char* path) {
		SdmcResult result = SdmcDirectoryExists(path);
		if (result != SDMC_SUCCESS) {
			return SDMC_SUCCESS;
		}

		nn::Result deleteResult = nn::fs::TryDeleteDirectoryRecursively(path);
		if (deleteResult.IsFailure()) {
			return SDMC_DELETE_FAILED;
		}

		return SDMC_SUCCESS;
	}

	const char* SdmcGetErrorString(SdmcResult result)
	{
		switch (result)
		{
		case SDMC_SUCCESS:
			return "Success";

		case SDMC_INVALID_ARGUMENT:
			return "Invalid argument";

		case SDMC_NOT_MOUNTED:
			return "SD card is not mounted";

		case SDMC_OPEN_FAILED:
			return "Failed to open file";

		case SDMC_READ_FAILED:
			return "Failed to read file";

		case SDMC_WRITE_FAILED:
			return "Failed to write file";

		case SDMC_PARTIAL_WRITE:
			return "Partial write";

		case SDMC_CREATE_DIRECTORY_FAILED:
			return "Failed to create directory";

		case SDMC_MOUNT_FAILED:
			return "Failed to mount SD card";

		case SDMC_STREAM_CLOSED:
			return "Stream is closed";

		case SDMC_FLUSH_FAILED:
			return "Failed to flush file";

		case SDMC_UNMOUNT_FAILED:
			return "Failed to unmount SD card";

		case SDMC_FILE_NOT_FOUND:
			return "File not found";

		case SDMC_INVALID_PATH:
			return "Path invalid";

		case SDMC_DELETE_FAILED:
			return "Failed to delete file";

		default:
			return "Unknown error";
		}
	}
}