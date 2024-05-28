export server="localhost"
export port="27017"
export database="AuctionDB"
export collection="auctionBidCol"
export rabbitMQPort="5672"
export redisConnection="redis-16065.c56.east-us.azure.redns.redis-cloud.com:16065,password=1234"
export Secret="RasmusGrønErSuperCoolOgDenBedsteChef!"
export Issuer="Gron&OlsenGruppen"
echo $database $collection
dotnet run server="$server" port="$port" collection="$collection" database="$database" Secret=$Secret Issuer=$Issuer 