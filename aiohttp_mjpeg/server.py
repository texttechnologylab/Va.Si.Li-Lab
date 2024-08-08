import logging
import os
import aiohttp
from aiohttp import web, MultipartWriter
import socket
import struct
import asyncio

logging.basicConfig()
log_level = os.environ.get("LOG_LEVEL", "INFO").upper()
logger = logging.getLogger("server")
log_level = getattr(logging, log_level)
logger.setLevel(log_level)


class Camera:

    def __init__(self, idx, conn):
        self._idx = idx
        self.conn = conn

        self.data = b''  # CHANGED
        self.payload_size = struct.calcsize("<L")  # CHANGED

    @property
    def identifier(self):
        return self._idx

    # The camera class should contain a "get_frame" method
    async def get_frame(self):

        while len(self.data) < self.payload_size:
            revivedData = self.conn.recv(4096)
            if(len(revivedData) == 0):
                self.stop()
                return
            self.data += revivedData

        packed_msg_size = self.data[:self.payload_size]
        self.data = self.data[self.payload_size:]
        msg_size = struct.unpack("<L", packed_msg_size)[0]  # CHANGED

        # Retrieve all data based on message size
        while len(self.data) < msg_size:
            revivedData = self.conn.recv(4096)
            if(len(revivedData) == 0):
                self.stop()
                return
            self.data += revivedData

        frame_data = self.data[:msg_size]
        self.data = self.data[msg_size:]

        # Extract frame
        return frame_data

    def stop(self):
        self.conn.close()
        del cam_routes[self._idx]


cam_routes = {}


class StreamHandler:

    async def __call__(self, request):
        camID = int(request.match_info['id'])

        my_boundary = 'image-boundary'
        response = web.StreamResponse(
            status=200,
            reason='OK',
            headers={
                'Content-Type': 'multipart/x-mixed-replace;boundary={}'.format(my_boundary)
            }
        )
        await response.prepare(request)
        while True:
            if(camID in cam_routes):
                self._cam = cam_routes[camID]
                self.frame = await self._cam.get_frame()
                with MultipartWriter('image/jpeg', boundary=my_boundary) as mpwriter:
                    mpwriter.append(self.frame, {
                        'Content-Type': 'image/jpeg'
                    })
                try:
                    await mpwriter.write(response, close_boundary=False)
                except ConnectionResetError:
                    logger.warning("Client connection closed")
                    break
                await response.write(b"\r\n")

            await asyncio.sleep(0.01)


class MjpegServer:

    def __init__(self, host='0.0.0.0', port='8080'):
        self._port = port
        self._host = host
        self._app = web.Application()
        print("starting socket ")
        self.s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.s.settimeout(0.01)
        self.s.bind(('', 8089))
        self.s.listen(10)

    async def root_handler(self, request):
        # TO-DO : load page with links
        text = 'Available streams:\n\n'
        for route in cam_routes:
            text += f"{route} \n"
        return aiohttp.web.Response(text=text)

    async def start(self):
        # Start API to recieve Stream
        self._app.router.add_route("GET", "/", self.root_handler)
        self._app.router.add_route("GET", '/cam/{id}', StreamHandler())
        runner = web.AppRunner(self._app)
        await runner.setup()
        site = web.TCPSite(runner, host=self._host, port=self._port)
        await site.start()

        # Listen for new webcam connections
        print("Waiting for socket connections")
        while True:
            try:
                conn, addr = self.s.accept()
                print(addr)
                camId = b''
                while len(camId) < 4:
                    camId += conn.recv(4)

                camIdInt = int.from_bytes(camId, "little")

                cam_routes[camIdInt] = Camera(camIdInt, conn)
                print("add new webcam with id " + str(camIdInt))
            except socket.timeout:
                await asyncio.sleep(0.1)

    def stop(self):
        web.delete
        pass
