"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").withAutomaticReconnect().build();
var isConnected = false;
var tid = null;
//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (user, message) {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    li.textContent = `${user} says ${message}`;
});

connection.on("activeUser", function (groups) {
    console.log('activeUsers', groups);
});
connection.on("newGroup", function (group) {
    console.log('newGroup', group);
    if (group.id) {
        group.message = 'Hi group';

        connection.invoke('GroupMessage', { name: 'jasim', email: 'jasim@gmail.com' }, group);
    }
});
connection.on("groupMessage", function (message) {
    console.log('GroupMessage', message);
});

connection.onclose(onClose)
function onClose() {
    isConnected = false;
    if (tid) {
        clearInterval(tid)
    }
    tid = setInterval(() => {
        if (!isConnected) {
            connection.start().then(() => {
                isConnected = true;
                clearInterval(tid)
            })
            .catch(err => console.error(err.toString()));
        }
    }, 5000)
}
connection.start().then(function () {
    isConnected = true;
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    console.log(err)
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    var message = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", user, message);
    event.preventDefault();
});

document.getElementById("start").addEventListener("click", function (event) {
    console.log('start1')
    //connection.start()
    connection.invoke('ActiveUser', { name: 'jasim', email: 'jasim@gmail.com' }, [{ name: 'omar', email: 'omar@gmail.com' },{ name: 'jasim', email: 'jasim@gmail.com' },{ name: 'arif', email: 'arif@gmail.com' }])
    event.preventDefault();
});

document.getElementById("stop").addEventListener("click", function (event) {
    console.log('stop...')
    var data = { id: '', groupName: 'g1', isPrivate: false, usersJson: JSON.stringify([{ name: 'omar', email: 'omar@gmail.com' }, { name: 'jasim', email: 'jasim@gmail.com' }]) };
    console.log(data)
    connection.invoke('NewGroup', { name: 'jasim', email: 'jasim@gmail.com' }, data)
});

window.onload = function () {
    console.log('document loaded')
    document.getElementById('bold').addEventListener('getColumns', ev => {
        ev.detail.signal.set(['Id', 'Name', 'Address'])
    })
}