import { useEffect, useRef } from 'react'
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr'
import {getUnreadStatus} from './state'

const useSignalrConnectionHook = (
  signalrServer: string,
  cb: (listenningon: string, arg: unknown) => void,
) => {
  const socketRef = useRef<HubConnection | null>(null)

  function socketClient() {
    let isConnected = false
    let tid: any
    const connection = new HubConnectionBuilder()
      .withUrl(signalrServer)
      .withAutomaticReconnect()
      .build()
    connection.on('connected', function () {
      cb('connected', '')
    })
    connection.on('activeUser', function (groups, unreadStatus) {
      const status=getUnreadStatus();
      Object.keys(status).forEach(key=>{
        if(unreadStatus[key]){
          unreadStatus[key]+=status[key];
        }else{
          unreadStatus[key]=status[key];
        }
      });
      cb('activeUser', {groups, unreadStatus})
    })
    connection.on('newGroup', function (group, creatorId) {
      group.creatorId=creatorId;
      cb('newGroup', group)
    })
    connection.on('groupMessage', function (message, groupName, isPrivate) {
      message.groupName=groupName;
      message.isPrivate=isPrivate;
      cb('groupMessage', message)
    })
    connection.on('ReceiveMessage', (user, message) => {
      cb('receiveMessage', `${user} says ${message}`)
    })
    connection.on('messagesByGroupId', (data, groupName) => {
      cb('messagesByGroupId', [data, groupName])
    })
    
    connection.onclose(() => {
      isConnected = false
      if (tid) {
        clearInterval(tid)
      }
      tid = setInterval(() => {
        if (!isConnected) {
          connection
            .start()
            .then(() => {
              isConnected = true;
              clearInterval(tid);
            })
            .catch((err) => console.error(err.toString()))
        }
      }, 2000)
    })
    
    connection
      .start()
      .then(function () {
        isConnected = true
      })
      .catch(function (err) {
        console.log(err)
        return console.error(err.toString())
      })

    socketRef.current = connection
  }

  useEffect(() => {
    socketClient()
  }, [])

  return socketRef
}

export default useSignalrConnectionHook
