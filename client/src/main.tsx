/*import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)*/
//import {cn} from './lib/utils'
import ChatWindow from '@/signalrClient/chatWindow'
import './index.css'
//import {Button} from './components/ui/button'
//import React from "react"
//import ReactDOM from "react-dom/client" 
import r2wc from '@r2wc/react-to-web-component'

/*const Greeting = ({ name }:{name:string}) => {
  return <Button>Hello -- , {name}!</Button>
}*/

const WebChatWindow = r2wc(ChatWindow, {
  props: {
    hubUrl: "string",
    currentUser:"json",
    userList:"json",
    hideWindow:"boolean",
    bufferHeight:"number",
    unreadStatusSignal:"function",
    popupMessage:"function",
  },
})

customElements.define("signalr-client", WebChatWindow)