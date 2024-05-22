export server="localhost"
export port="27017"
export database="AuctionDB"
export collection="auctionCol"
export rabbitMQPort="5672"
export redisConnection="redis-18021.c251.east-us-mz.azure.redns.redis-cloud.com:18021,password=1234"
echo $database $collection
dotnet run server="$server" port="$port" auctionCol="$collection" database="$database"