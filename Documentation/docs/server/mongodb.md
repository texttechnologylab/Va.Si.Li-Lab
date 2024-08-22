# MongoDB
We use a MongoDB to store the experiments and the recorded data of all participants.

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
  bindIp: 127.0.0.1

# security
security.authorization : enabled
```
