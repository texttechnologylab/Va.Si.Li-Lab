# Introduction
This repo implements a very simple mjpeg server for streaming from one or multiple cameras. The repo has two files
 1) server.py - Implementation of the mjpeg server
 2) main.py - Demonstration of usage
 
 # How to use
 The class ```MjpegSever``` implements a simple mjpeg server. In order to use the class, create an instance of the class and add the necessary routes as follows
 ```python
 server = MjpegServer(host, port)
 # default host - 0.0.0.0
 # defaul port = 8080
 server.add_stream("cam_route", Camera())
 server.start()
 ```
 The class ```Camera``` should have a ```get_frame``` method which returns the encoded jpeg. An example can be seen in ```main.py```. Once the server is running, the stream can be accessed at ```http://localhost:8080/cam_route```
