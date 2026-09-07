# MongoDB
We use a MongoDB to store the experiments and the recorded data of all participants.
It is written to exclusively by the [Database API](python_data_logger.md) - nothing
else connects to it directly.

!!! info "Collections, indexes and timestamps"
    The collections the API creates, the indexes it declares on startup, and the
    difference between the client-side `localTime` and the server-assigned
    `serverTime` are documented at
    **[Data Model](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/data_model/)**
    in the backend documentation.

    That distinction matters for analysis: `serverTime` is stamped when a
    *batched* request arrives, so every sample in one batch shares it. Use
    `localTime` for alignment within a client.

## Installation

### Docker
You can run a MongoDB instance using Docker. You should adapt all paths and logins to your needs.

```yaml title="example.yaml to create a MongoDB container" linenums="1" hl_lines="9-10 14-17"
version: '3'
services:
  mongodb_container:
    image: mongo:latest
    container_name: vasililab_mongodb
    hostname: mongodb
    restart: always
    environment:
      MONGO_INITDB_ROOT_USERNAME: root #(1)
      MONGO_INITDB_ROOT_PASSWORD: root
    ports:
      - "27017:27017"
    volumes:
      - /vol/mongoVaSiLiLab/data/:/data/db/ #(2)
      - /vol/mongoVaSiLiLab/mongod.conf:/etc/mongod.conf
      - /vol/mongoVaSiLiLab/log/:/var/log/mongodb/
      - /vol/mongoVaSiLiLab/mongohome/:/home/mongodb/
    command: [ "-f", "/etc/mongod.conf" ]

volumes:
  mongodb_data_container:
```
    
1.  Customise the data according to your needs.
2.  Change the directories according to your system.

!!! warning "`bindIp: 127.0.0.1` only accepts local connections"
    With this setting MongoDB is reachable only from the container itself, so an
    API running elsewhere cannot connect. Widen it to the interface the API
    reaches it on - and keep `security.authorization` enabled if you do.


```yaml title="mongod.conf" linenums="1" hl_lines="16-17"
# for documentation of all options, see:
#   http://docs.mongodb.org/manual/reference/configuration-options/

# Where and how to store data.
storage:
  dbPath: /data/db

# where to write logging data.
systemLog:
  destination: file
  logAppend: true
  path: /var/log/mongodb/mongod.log

# network interfaces
net:
  port: 27017
  bindIp: 127.0.0.1 #(3)

# security
security.authorization : enabled
```
