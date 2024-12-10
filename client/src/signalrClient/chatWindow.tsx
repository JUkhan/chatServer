
import React, { useEffect, useRef, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Plus, CirclePlus } from 'lucide-react'
import { cn } from '@/lib/utils'
import MarkdownView from 'react-showdown'
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from '@/components/ui/resizable'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { Toaster } from "@/components/ui/sonner"
import { ScrollArea } from '@/components/ui/scroll-area'
import GroupChatComponent from './group'
import { toast } from 'sonner'
import { saveToLocal } from './state'
import useSignalrConnectionHook from './useSignalrConnection'
import { format } from 'date-fns/format'
import { getTime } from 'date-fns/getTime'
import { Badge } from '@/components/ui/badge'


type User = { name: string; email: string }
type Params = {
  hubUrl: string
  currentUser: User
  userList: User[]
  hideWindow: boolean
  bufferHeight: number
  unreadStatusSignal: (status: Record<string, number>) => void,
  popupMessage: (ev:{title: string, message: string}) => void
}
type Group = {
  id: string
  groupName: string
  isPrivate: boolean
  usersJson: string
  message?: string
  [key: string]: any
}

const ChatWindow: React.FC<Params> = ({
  hubUrl,
  currentUser,
  userList,
  hideWindow = true,
  bufferHeight = 0,
  unreadStatusSignal,
  popupMessage,
}) => {
  const [{ groups, selectedGroup, msgObj, unreadStatus }, setState] = useState<{
    groups: Group[]
    selectedGroup: Group
    msgObj: any
    unreadStatus: Record<string, number>
  }>({ groups: [], selectedGroup: {} as any, msgObj: {}, unreadStatus: {} })
  
  const io = useSignalrConnectionHook(hubUrl, (listenningOn: string, data: any) => {
    switch (listenningOn) {
      case 'connected':
        io.current?.invoke('ActiveUser', currentUser, userList)
        break
      case 'activeUser':
        setState((pre) => ({ ...pre, groups: data.groups, unreadStatus: data.unreadStatus }))
        if (typeof unreadStatusSignal === 'function') {
          unreadStatusSignal(data.unreadStatus)
        }
        break
      case 'messagesByGroupId':
        setState((pre) => ({
          ...pre,
          msgObj: { ...pre.msgObj, [data[1]]: data[0] },
        }))
        scrollToView();
        break
      case 'groupMessage':
        setState((pre) => {
          const len = pre.msgObj[data.groupName] ? pre.msgObj[data.groupName].length : 0
          if (pre.msgObj[data.groupName] && len > 0) {
            const createdAt = pre.msgObj[data.groupName][len - 1].createdAt
            if (createdAt === data.createdAt) {
              return pre
            }
          }
          const newObj = Object.assign({}, pre)
          const gname:string=data.groupName;
          if(gname.endsWith("All Users")){
            if(!pre.groups[0].groupName.includes(gname.replace("All Users", ''))){return pre;}
          }
          if (newObj.msgObj[data.groupName]) {
            newObj.msgObj[data.groupName] = [...newObj.msgObj[data.groupName], data]
            scrollToView();
          } else {
              newObj.msgObj[data.groupName] = [data]
              io.current?.invoke(
                'MessagesByGroupId',
                pre.groups.find((it) => it.groupName === data.groupName),
              );
          }
          if (pre.selectedGroup.groupName !== data.groupName) {
            const st = pre.unreadStatus[data.groupName] || 0
            newObj.unreadStatus = { ...newObj.unreadStatus, [data.groupName]: st + 1 }
            if (typeof popupMessage === 'function'){
              popupMessage({title:`${getGroupName(data)} - ${data.userName}`,  message:data.message })
            }else{
              toast(`${getGroupName(data)} - ${data.userName}`, {
                description: data.message,
              })
            }
            if (typeof unreadStatusSignal === 'function') {
              setTimeout(() => {
                unreadStatusSignal(newObj.unreadStatus)
              }, 300)
            }
          }
          return newObj
        })
        break
      case 'newGroup':
        if(data.id){
          const match=data.groupName.match(/\$@&\$.+\$@&\$/)[0];
        setState((pre) => {
          if(!pre.groups[0].groupName.includes(match)){return pre;}
          if (!pre.groups.find((it) => it.groupName === data.groupName)) {
            if (data.usersJson.includes(currentUser.email)) {
              const groups = [...pre.groups, data]
              let sg = pre.selectedGroup
              if (data.creatorId === currentUser.email) {
                sg = data
              } //else {
                io.current?.stop()
              //}
              return { ...pre, groups, selectedGroup: sg }
            }
          }
          return pre
        });}else{
          if (typeof popupMessage === 'function'){
            popupMessage({title:data.groupName,  message:'Already exist' })
          }else{
            toast.info(`"${data.groupName}" already exist`)
          }
        }
        break
    }
  })
  const [message, setMessage] = useState('')
  const [[width, height], setHeight] = useState([0, 0])
  const [isPrivate, setPrivate] = useState(0)
  const scrollElmRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (scrollElmRef.current) {
      scrollElmRef.current.scrollIntoView()
    }
    setHeight(() => [inputRef.current?.clientWidth ?? 700, window.innerHeight - bufferHeight])
    const handleSize = () => {
      setHeight(() => [inputRef.current?.clientWidth ?? 700, window.innerHeight - bufferHeight])
    }
    window.addEventListener('resize', handleSize);
    return () => {
      window.removeEventListener('resize', handleSize)
      saveToLocal(unreadStatus)
    }
  }, [io, bufferHeight, unreadStatus])
  const scrollToView = (delay: number = 100) => {
    setTimeout(() => {
      if (scrollElmRef.current) {
        scrollElmRef.current.scrollTop = scrollElmRef.current.scrollHeight;
      }
    }, delay)
  }
  const sendMessage = () => {
    io.current?.invoke(
      'GroupMessage',
      currentUser,
      {
        ...selectedGroup,
        message,
      } as Group,
    )
    setMessage('')
  }
  const userName = `${currentUser?.name}`;
  const getGroupName=(group:Group)=>{
    const name=(group?.groupName??'').replace(/\$@&\$.+\$@&\$/,'')
    return group.isPrivate? name.replace(userName, '').replace(',', ''): name
  }
  
  return (
    <><Toaster/>
    <TooltipProvider>
      <div style={{ height: hideWindow ? 0 : height }} className="ml-2 mr-2 mb-2">
        <ResizablePanelGroup direction="horizontal" className="rounded border">
          <ResizablePanel defaultSize={25} className="bg-gray-50">
            <div className="h-12 border-b">
              <div className="flex items-center justify-between">
                <span className="font-semibold pt-3 pl-4">Chat</span>
                <div className="flex pt-2 pr-2">
                  <Tooltip>
                    <TooltipTrigger>
                      <CirclePlus
                        onClick={() => setPrivate(1)}
                        className="hover:text-red-400 cursor-pointer mr-2 w-4 h-4"
                      />
                    </TooltipTrigger>
                    <TooltipContent>
                      <p>Private chat</p>
                    </TooltipContent>
                  </Tooltip>
                  <Tooltip>
                    <TooltipTrigger>
                      <Plus
                        onClick={() => setPrivate(2)}
                        className="hover:text-red-400 cursor-pointer w-4 h-4"
                      />
                    </TooltipTrigger>
                    <TooltipContent>
                      <p>Add a group</p>
                    </TooltipContent>
                  </Tooltip>
                </div>
              </div>
            </div>
            <ScrollArea style={{ height: height - 120 }} className="">
              {groups?.map((group) => (
                <div
                  key={group.id}
                  onClick={() => {
                    if (!msgObj[group.groupName]) {
                      io.current?.invoke('MessagesByGroupId', group)
                    }
                    setState((pre) => ({
                      ...pre,
                      selectedGroup: group,
                      unreadStatus: { ...pre.unreadStatus, [group.groupName]: 0 },
                    }))
                    scrollToView()
                    if (inputRef.current) inputRef.current.focus()
                  }}
                  className={cn('p-2 cursor-pointer pl-4 font-semibold hover:bg-purple-100', {
                    'bg-purple-200': group.id === selectedGroup.id,
                  })}
                >
                  <span>{getGroupName(group)}</span>
                  {unreadStatus[group.groupName] > 0 && (
                    <Badge className="ml-2" variant="destructive">
                      {unreadStatus[group.groupName]}
                    </Badge>
                  )}
                </div>
              ))}
            </ScrollArea>
          </ResizablePanel>
          <ResizableHandle />
          <ResizablePanel className="bg-white" defaultSize={75}>
            <div className="flex items-center pl-4 h-12 border-b">
              {isPrivate === 0 ? (
                <span>{getGroupName(selectedGroup)}</span>
              ) : (
                <GroupChatComponent
                  currentUser={currentUser}
                  users={userList}
                  isPrivate={isPrivate === 1}
                  setPrivate={setPrivate}
                  socket={io.current!}
                />
              )}
            </div>

            <ScrollArea id="scroll-area" ref={scrollElmRef} className="p-2 pb-8" style={{ height: height - 100 }}>
              <div className="flex flex-col items-center">
                <div style={{ width: width + 60 }}>
                  {msgObj[selectedGroup.groupName]?.map((m: any, i: number) => (
                    <div key={m.id}>
                      <div
                        className={cn({
                          'flex flex-col items-end justify-end': m.userName === userName,
                        })}
                      >
                        {i > 0 &&
                        m.userName == msgObj[selectedGroup.groupName][i - 1].userName &&
                        (getTime(new Date(m.createdAt)) -
                          getTime(new Date(msgObj[selectedGroup.groupName][i - 1].createdAt))) /
                          60000 <
                          5.0 ? null : (
                          <div className="text-xs mt-2">
                            {m.userName}{' '}
                            <i className="text-slate-500">
                              {format(new Date(m.createdAt), 'eeee MM/dd/yyyy pp')}
                            </i>
                          </div>
                        )}
                        <div
                          className={cn(
                            'bg-lime-300 p-2 pl-3 pr-3 rounded mt-1 inline-block max-w-[800px]',
                            { 'bg-slate-200': m.userName === userName },
                          )}
                        >
                          <MarkdownView
                            markdown={m.message}
                            options={{ tables: true, emoji: true }}
                          />
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </ScrollArea>

            <div className="flex items-center justify-center">
              <Input
                ref={inputRef}
                className="inline-block sm:w-[400px] md:w-[500px] lg:w-[1000px]"
                value={message}
                placeholder="Type a message"
                onKeyUp={(ev) => {
                  if (ev.key === 'Enter' && !!(selectedGroup?.id && message && currentUser)) {
                    sendMessage()
                  }
                }}
                onChange={(ev) => setMessage(ev.target.value)}
              ></Input>
              <Button
                disabled={!(selectedGroup?.id && message && currentUser)}
                className="primary"
                onClick={sendMessage}
              >
                Send
              </Button>
            </div>
          </ResizablePanel>
        </ResizablePanelGroup>
      </div>
    </TooltipProvider>
    </>
  )
}

export default ChatWindow
