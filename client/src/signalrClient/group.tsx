"use client";

import React, {useState } from "react";
import { Input } from "@/components/ui/input";
import { MultiSelect } from "@/components/ui/multi-select";
import { Button } from "@/components/ui/button";
import { Combobox } from "@/components/ui/combobox";
import { HubConnection } from "@microsoft/signalr";
//import { toast } from "sonner";
//import {useSelector} from './state'

const GroupChatComponent = ({
  isPrivate,
  setPrivate,
  socket,
  currentUser,
  users
}: {
  isPrivate: boolean;
  setPrivate: any;
  socket: HubConnection;
  currentUser: any,
  users:any[]
}) => {
  //const { loggedInUser, createGroup,  users} = useSelector(s=>s);
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [group, setGroup] = useState("");
  const [privateUserId, setValue] = React.useState("");
  const dataList = React.useMemo(() => users.map(it => ({label: it.name, value: it.email})), [users]);
  const submitHandler = async () => {
    const item = {
      id:'',
      isPrivate,
      groupName: group,
      usersJson: JSON.stringify(users.filter(it=>selectedUsers.includes(it.email))),
    };
    socket.invoke("NewGroup", currentUser, item);
    setPrivate(0);
  };

  const submitPrivateHandler = async () => {
    const anotherUser = dataList.find((it) => it.value === privateUserId);
    const names: any = [anotherUser?.label, currentUser?.name];
    const item = {
      id:'',
      groupName: names.toSorted().join(","),
      isPrivate: true,
      usersJson: JSON.stringify(users.filter(it=>[currentUser.email, anotherUser?.value].includes(it.email))),
    };
    socket.invoke("NewGroup", currentUser, item);
    setPrivate(0);
  };

  return (
    <div className="flex items-center gap-2">
      {!isPrivate ? (
        <>
          <Input
            placeholder="Group Name"
            className="w-[200px]"
            value={group}
            onChange={(ev) => setGroup(ev.target.value)}
          />
          <MultiSelect
            className="min-w-[150px]"
            options={dataList ?? []}
            onValueChange={setSelectedUsers}
            placeholder="Select Users"
            variant="inverted"
            maxCount={1}
          />
        </>
      ) : (
        <Combobox
          items={dataList?.filter((it) => it.value !== currentUser?.email) as any}
          value={privateUserId}
          setValue={setValue}
        />
      )}
      <Button
        disabled={
          isPrivate
            ? !privateUserId
            : !(group && selectedUsers && selectedUsers.length > 1)
        }
        size="sm"
        onClick={() => (isPrivate ? submitPrivateHandler() : submitHandler())}
      >
        Submit
      </Button>
      <Button size="sm" variant="outline" onClick={() => setPrivate(0)}>
        X
      </Button>
    </div>
  );
};

export default GroupChatComponent;
