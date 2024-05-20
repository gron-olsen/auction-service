@description('Name for the container group')
param name string = 'auction-service'

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Port to open on the container and the public IP address.')
param port int = 80

@description('The number of CPU cores to allocate to the container.')
param cpuCores int = 1

@description('The amount of memory to allocate to the container in gigabytes.')
param memoryInGb int = 2

@description('The behavior of Azure runtime if container has stopped.')
@allowed([
  'Always'
  'Never'
  'OnFailure'
])
param restartPolicy string = 'Always'

@description('Specifies the name of the Azure Storage account.')
param storageAccountName string = 'storage${uniqueString(resourceGroup().id)}'

@description('Specifies the prefix of the file share names.')
param sharePrefix string = 'storage'

@description('List of file shares to create')
var shareNames = [
  'dbdata'
]

//--- Opret storage konto til volumener ---
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
  }
}

//-- Opret et share i storage account til MongoDB data og Image upload data
resource service 'Microsoft.Storage/storageAccounts/fileServices@2023-04-01' = {
  parent: storageAccount
  name: 'default'
}

resource fileshare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-04-01' = [for share in shareNames : {
  parent: service
  name: '${sharePrefix}${share}'
}]


resource containerGroup 'Microsoft.ContainerInstance/containerGroups@2023-04-01' = {
  name: name
  location: location
  properties: {
    containers: [
      {
        name: name
        properties: {
          environmentVariables: [

            {
              name: 'MongoConnectionString'
              value: 'mongodb://auctionUser:user12E4@localhost:27017/?authSource=admin'
            }
            {
              name: 'auctionDatabase'
              value: 'auction'
            }
            {
              name: 'auctionCollection'
              value: 'products'
            }
          ]
          ports: [
            {
              port: port
              protocol: 'TCP'
            }
          ]
          resources: {
            requests: {
              cpu: cpuCores
              memoryInGB: memoryInGb
            }
          }
          volumeMounts: [
            {
              name: 'images'
              mountPath: '/srv/resources/images'
            }
          ]
        }
      }
      {
        name: 'mongodb'
        properties: {
          image: 'mongo:latest'
          command: [
            'mongod'
            '--dbpath=/data/mongodb'
            '--bind_ip_all'
            '--auction'
          ]
          ports: [
            {
              protocol: 'TCP'
              port: 27017
            }
          ]
          environmentVariables: [
          ]
          resources: {
            requests: {
              memoryInGB: json('1.0')
              cpu: json('0.5')
            }
          }
          volumeMounts: [
            {
              name: 'db'
              mountPath: '/data/mongodb'
            }
          ]
        }
      }
    ]
    osType: 'Linux'
    volumes: {
        name: 'db'
        azureFile: {
          shareName: 'storagedbdata'
          storageAccountName: storageAccount.name
          storageAccountKey: storageAccount.listKeys().keys[0].value
        }
      }
     
      
    restartPolicy: restartPolicy
    ipAddress: {
      type: 'Public'
      dnsNameLabel: 'qgtauctionsvc'
      ports: [
        {
          port: port
          protocol: 'TCP'
        }
        {
          port: 27017
          protocol: 'TCP'          
        }
      ]
    }
  }
}

output containerIPv4Address string = containerGroup.properties.ipAddress.ip
