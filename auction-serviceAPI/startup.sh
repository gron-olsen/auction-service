export server="localhost"
export port="27017"
export database="AuctionDB"
export collection="auctionCol"
echo $database $collection
dotnet run server="$server" port="$port" auctionCol="$collection" database="$database"