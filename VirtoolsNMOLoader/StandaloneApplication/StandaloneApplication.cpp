#include "main.h"
#include <iostream>
#include <WinSock2.h>
#include <Ws2tcpip.h> 
#include <string>
#include <intrin.h>
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "ws2_32.lib")
#pragma comment(lib, "netapi32.lib")


#define DEFAULT_PORT 2566// Default port.
#define BACKLOG 5/* Maximum pending connections. */
#define RECV_BUFFER_SIZE 512// Receive/send buffer size.
#define SEND_BUFFER_SIZE 2048// Receive/send buffer size.

SOCKET sockfd, new_fd; /* Listening socket and accepted connection socket. */
struct sockaddr_in my_addr; /* Local address structure, populated below. */
struct sockaddr_in their_addr; /* Remote address information. */
int port = DEFAULT_PORT;
int sin_size;
int timeout = 300;
int buf = 10240;
HANDLE hThread = nullptr;
bool running = false;

enum SendBackInfoType {
  SendBackInfoTypeInt,
  SendBackInfoTypePtr,
};
struct SendBackInfoHeader {
  int success;
  int bufferType;
  long long bufferSize;
};

std::string IPToString(sockaddr_in* addr)
{
  char sendBuf[100] = { '\0' };
  inet_ntop(AF_INET, (void*)&addr->sin_addr, sendBuf, 100);
  return std::string(sendBuf);
}

bool serverLoop()
{
  char recv_buf[RECV_BUFFER_SIZE];
  char send_buf[SEND_BUFFER_SIZE];

  printf_s("Server work start\n");

  int recvTimeout = 30 * 1000;
  int sendTimeout = 30 * 1000;

  int len = sizeof(SOCKADDR);
  new_fd = accept(sockfd, (SOCKADDR*)&their_addr, &len);
  // Block here while waiting.

  if (new_fd == SOCKET_ERROR)
  {
    printf_s("receive failed\n");
    closesocket(sockfd);
    return false;
  }

  // Set the timeout.
  setsockopt(new_fd, SOL_SOCKET, SO_RCVTIMEO, (char*)&recvTimeout, sizeof(int));
  setsockopt(new_fd, SOL_SOCKET, SO_SNDTIMEO, (char*)&sendTimeout, sizeof(int));

  printf_s("New connection: %s\n", IPToString(&their_addr).c_str());

  while (running) {

    memset(recv_buf, 0, sizeof(recv_buf));

    int recv_len = recv(new_fd, recv_buf, RECV_BUFFER_SIZE, 0);
    if (recv_len <= 0) // Continue listening after a client disconnects; keep the server running.
    {
      closesocket(new_fd);
      printf_s("Connection close\n");

      sin_size = sizeof(struct sockaddr_in);
      new_fd = accept(sockfd, (struct sockaddr*)&their_addr, &sin_size);// Block until data arrives; arguments are the socket, receive buffer, and size.
      if (new_fd == SOCKET_ERROR) {
        printf_s("Server receive failed\n");
        return false;
      }
      else {

        // Set the timeout.
        setsockopt(new_fd, SOL_SOCKET, SO_RCVTIMEO, (char*)&recvTimeout, sizeof(int));
        setsockopt(new_fd, SOL_SOCKET, SO_SNDTIMEO, (char*)&sendTimeout, sizeof(int));

        printf_s("New connection: %s\n", IPToString(&their_addr).c_str());
        continue;
      }
    }

    bool sendBack = false;
    size_t sendBackSize = SEND_BUFFER_SIZE;
    SendBackInfoHeader sendBackHeader = { 0 };

    if (strcmp(recv_buf, "") != 0) {
      // Process API data.
      if (strncmp(recv_buf, "Loader_GetLastError", 20) == 0) {
        int result = Loader_GetLastError();
        sendBackHeader.bufferSize = sizeof(int);
        sendBackHeader.bufferType = SendBackInfoTypeInt;
        sendBack = true;
        memcpy_s(send_buf, sizeof(send_buf), &sendBackHeader, sizeof(sendBackHeader));
        memcpy_s(send_buf + sizeof(sendBackHeader), sizeof(send_buf) - sizeof(sendBackHeader), &result, sizeof(result));
      }
      else if (strncmp(recv_buf, "Loader_SolveNmoFileRead", 24) == 0) {
        int resultCode = 0;
        void* result = Loader_SolveNmoFileRead(recv_buf + 25, &resultCode);
        void* ptr = send_buf + sizeof(sendBackHeader);
        sendBackHeader.bufferSize = sizeof(int);
        sendBackHeader.bufferType = SendBackInfoTypeInt;
        sendBack = true;
        memcpy_s(send_buf, sizeof(send_buf), &sendBackHeader, sizeof(sendBackHeader));
        memcpy_s(ptr, sizeof(send_buf), &result, sizeof(result)); 
        ptr = (void*)((intptr_t)ptr + sizeof(result));
        memcpy_s(ptr, sizeof(send_buf), &resultCode, sizeof(resultCode));
      }
      
      
    }
  SEND:
    if (sendBack) {
      if (send(new_fd, send_buf, sendBackSize, 0) == -1)
        printf_s("%s -> Send failed! \n", recv_buf);
    }
  }

  printf_s("Server Quit\n");

  closesocket(new_fd);
  closesocket(sockfd);
  return true;
}
bool startServer() {
  WSADATA wsaData;
  if (WSAStartup(MAKEWORD(2, 0), &wsaData) != 0)
  {
    printf_s("WSAStartup failed: %d", WSAGetLastError());
    return false;
  }

  sockfd = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);// Create the socket.
  if (sockfd == SOCKET_ERROR) {
    printf_s("socket failed : % d", errno);
    return false;
  }

  my_addr.sin_family = AF_INET;/* Accept traffic from this machine or other machines. */
  my_addr.sin_port = htons(port);/* Port number. */
  my_addr.sin_addr.s_addr = htonl(INADDR_ANY);/* IP address; the value selects the local interface. */
  ZeroMemory(&my_addr.sin_zero, sizeof(my_addr.sin_zero));/* Zero the remaining fields. */

  if (bind(sockfd, (struct sockaddr*)&my_addr, sizeof(struct sockaddr)) < 0) {// Bind the address to the socket.
    printf_s("bind error");
    return false;
  }

  if (listen(sockfd, BACKLOG) == SOCKET_ERROR)// Start listening; the second argument is the connection backlog.
  {
    printf_s("listen error");
    return false;
  }

  int recvTimeout = 10 * 1000;
  int sendTimeout = 10 * 1000;

  setsockopt(sockfd, SOL_SOCKET, SO_RCVTIMEO, (char*)&recvTimeout, sizeof(int));
  setsockopt(sockfd, SOL_SOCKET, SO_SNDTIMEO, (char*)&sendTimeout, sizeof(int));


  return true;
}
bool stopServer()
{
  if (running) {
    running = false;
  }
  if (sockfd > 0) {
    shutdown(sockfd, SD_BOTH);
    closesocket(sockfd);
    sockfd = 0;
  }
  WSACleanup();
  return false;
}

#define CKCID_GROUP 23
#define CKCID_MATERIAL 30
#define CKCID_TEXTURE 31
#define CKCID_MESH 32
#define CKCID_3DENTITY 33
#define CKCID_3DOBJECT 41

// Main entry point.

int main(int argc, char const* argv[])
{
  // Path.
  char fullPath[512];
  GetModuleFileName(0, fullPath, 512);
  for (int i = strlen(fullPath) - 1; i >= 0; i--)
    if (fullPath[i] == '\\') {
      fullPath[i + 1] = '\0';
      printf_s("Full path is : %s\n", fullPath);
      break;
    }

#if 1
  char recv_buf[15];
  sprintf_s(recv_buf, "Test string");

  // Initialize the library.
  if (Loader_Init(GetConsoleWindow(), fullPath))
    return -1;

  char filePath[512];
  strcpy(filePath, fullPath);
  strcat(filePath, "\\1.NMO");

  // Read the file.
  void* nmoFile = Loader_SolveNmoFileRead(filePath, nullptr);
  if (!nmoFile) {
    printf("Load failed");
    return -1;
  }

  // Iterate file objects.
  int classId = 0;
  void* objPtr = 0;
  char objName[512];
  int index = 0;
  do {
    objPtr = Loader_SolveNmoFileGetNext(nmoFile, &classId, objName);
    printf("%d\n", index++);

    switch (classId)
    {
    case CKCID_3DENTITY:
      printf("  CKCID_3DENTITY: %s\n", objName);
      break;
    case CKCID_3DOBJECT:
      printf("  CKCID_3DOBJECT: %s\n", objName);
      break;
    case CKCID_MESH:
      printf("  CKCID_MESH: %s\n", objName);
      break;
    case CKCID_MATERIAL:
      printf("  CKCID_MATERIAL: %s\n", objName);
      break;
    case CKCID_TEXTURE: {
      Loader_SolveNmoFileTexture(objPtr);
      printf("  CKCID_TEXTURE: %s\n", objName);
      break;
    }
    case CKCID_GROUP: {
      printf("  CKCID_GROUP: %s\n", objName);
      int groupObjCount = Loader_CKGroupGetObjectCount(objPtr);
      char objGroupObjName[512];
      int objGroupClassId = 0;
      for (int i = 0; i < groupObjCount; i++)
      {
        void* objGroupPtr = Loader_CKGroupGetObject(objPtr, i, &objGroupClassId, objGroupObjName);
        if (objGroupPtr && objGroupClassId == CKCID_3DOBJECT) {
          printf("    GroupObject: %s\n", objGroupObjName);
        }
      }
      break;
    }
    default:
      printf("  UNKNOW CKCID: %d %s\n", classId, objName);
      break;
    }

  } while (objPtr);


  // Close the file.
  Loader_SolveNmoFileDestroy(nmoFile);

  // Unload.
  return Loader_Destroy();
#endif
#if 0
  return 0;

  // Arguments.
  for (int i = 1; i < argc; i++)
  {
    // Port argument.
    if (strcmp(argv[i - 1], "-port") == 0 && strcmp(argv[i], "") != 0) {
      port = atoi(argv[i]);
      printf_s("Args port: %d\n", port);
    }
  }

  // Initialize the library.
  if (Loader_Init(GetConsoleWindow(), fullPath))
    return -1;

  // Start the server.
  if (!startServer())
    return -1;

  if (!serverLoop())
    return -1;


  // Stop the service.
  stopServer();

  // Unload.
  return Loader_Destroy();
#endif
}