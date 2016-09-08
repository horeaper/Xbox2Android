#include <fcntl.h>
#include <linux/input.h>
#include <android/log.h>

#define LOGI(...) ((void)__android_log_print(ANDROID_LOG_INFO, "Xbox2AndroidClientLibrary", __VA_ARGS__))
#define LOGW(...) ((void)__android_log_print(ANDROID_LOG_WARN, "Xbox2AndroidClientLibrary", __VA_ARGS__))
#define LOGE(...) ((void)__android_log_print(ANDROID_LOG_WARN, "Xbox2AndroidClientLibrary", __VA_ARGS__))

#pragma pack(push, 1)
struct InputEvent
{
	short type;
	short code;
	short value;
};
#pragma pack(pop)

int fd = -1;
input_event *eventBuffer;

extern "C" void TouchInjector_Init()
{
	eventBuffer = (input_event*)malloc(sizeof(input_event) * 1024);
	memset(eventBuffer, 0, sizeof(input_event) * 1024);
}

extern "C" bool TouchInjector_OpenDevice(const char *deviceName)
{
	fd = open(deviceName, O_WRONLY);
	return fd >= 0;
}

extern "C" void TouchInjector_Close()
{
	free(eventBuffer);
	if (fd != -1) {
		close(fd);
		fd = -1;
	}
}

extern "C" void TouchInjector_InjectEvent(const InputEvent *data, int count)
{
	memset(eventBuffer, 0, sizeof(input_event) * count);
	for (int cnt = 0; cnt < count; ++cnt) {
		eventBuffer[cnt].type = data[cnt].type;
		eventBuffer[cnt].code = data[cnt].code;
		eventBuffer[cnt].value = data[cnt].value;
	}
	write(fd, eventBuffer, sizeof(input_event) * count);
}
