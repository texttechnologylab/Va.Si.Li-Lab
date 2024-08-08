import logging
import os
import asyncio
from signal import SIGINT, SIGTERM

from server import MjpegServer

logging.basicConfig()
log_level = os.environ.get("LOG_LEVEL", "INFO").upper()
logger = logging.getLogger("mjpeg")
log_level = getattr(logging, log_level)
logger.setLevel(log_level)

if __name__ == "__main__":

    # Instantiate Server
    server = MjpegServer()

    try:
        # start server
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        loop.run_until_complete(server.start())
        # server.start()

    except KeyboardInterrupt:
        logger.warning("Keyboard Interrupt, exiting...")
    finally:
        server.stop()
        # for cam in cams.values():
        #    cam.stop()
