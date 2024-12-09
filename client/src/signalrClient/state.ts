const KEY='chat-unread-status'
export function getUnreadStatus():Record<string, number>{
    const localState=localStorage.getItem(KEY)||'{}'
    return JSON.parse(localState)
}
export function saveToLocal(status:Record<string, number>){
  localStorage.setItem(KEY, JSON.stringify(status))
}