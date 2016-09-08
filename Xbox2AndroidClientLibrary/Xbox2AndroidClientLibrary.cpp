#include <fcntl.h>
#include <linux/input.h>
#include <android/log.h>

#define LOGI(...) ((void)__android_log_print(ANDROID_LOG_INFO, "Xbox2AndroidClientLibrary", __VA_ARGS__))
#define LOGW(...) ((void)__android_log_print(ANDROID_LOG_WARN, "Xbox2AndroidClientLibrary", __VA_ARGS__))
#define LOGE(...) ((void)__android_log_print(ANDROID_LOG_WARN, "Xbox2AndroidClientLibrary", __VA_ARGS__))

struct InputEvent
{
	short type;
	short code;
	short value;
};

int fd;
input_event *eventBuffer;

extern "C" bool Xbox2AndroidClientLibrary_Init(const char *deviceName)
{
	eventBuffer = (input_event*)malloc(sizeof(input_event) * 1024);
	memset(eventBuffer, 0, sizeof(input_event) * 1024);

	fd = open(deviceName, O_WRONLY);
	return fd >= 0;
}

extern "C" void Xbox2AndroidClientLibrary_Close()
{
	free(eventBuffer);
	close(fd);
}

extern "C" void Xbox2AndroidClientLibrary_InjectEvent(const InputEvent *data, int count)
{
	memset(eventBuffer, 0, sizeof(input_event) * count);
	for (int cnt = 0; cnt < count; ++cnt) {
		eventBuffer[cnt].type = data[cnt].type;
		eventBuffer[cnt].code = data[cnt].code;
		eventBuffer[cnt].value = data[cnt].value;
	}
	write(fd, eventBuffer, sizeof(input_event) * count);
}
