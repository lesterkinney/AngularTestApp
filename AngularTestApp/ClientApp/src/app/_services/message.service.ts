import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { getPaginatedResult, getPaginationHeaders } from './paginationHelper';
import { Message } from '../_models/message';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr'
import { User } from '../_models/user';
import { BehaviorSubject } from 'rxjs';
import { take } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  url = environment.apiUrl + 'messages/';
  hubUrl = environment.hubUrl;
  private hubConnection: HubConnection;
  private messageThreadSource = new BehaviorSubject<Message[]>([]);
  messageThread$ = this.messageThreadSource.asObservable();

  constructor(private http: HttpClient) { }

  createHubConnection(user: User, otherUsername: string) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl + 'message?user=' + otherUsername, {
        accessTokenFactory: () => user.token
      })
      .withAutomaticReconnect()
      .build()

    this.hubConnection.start().catch(error => console.log(error));

    this.hubConnection.on('ReceiveMessageThread', messages => {
      this.messageThreadSource.next(messages);
    })

    this.hubConnection.on('NewMessage', message => {
      this.messageThread$.pipe(take(1)).subscribe(messages => {
        this.messageThreadSource.next([...messages, message])
      })
    })

    this.hubConnection.on('UpdatedGroup', group => {
      if(group.connections.some(x => x.userName === otherUsername)){
        this.messageThread$.pipe(take(1)).subscribe(messages => {
          messages.forEach(message => {
            if(!message.dateRead) {
              message.dateRead = new Date(Date.now())
            }
          })
          this.messageThreadSource.next([...messages])
        })
      }
    })
  }

  stopHubConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  getMessages(pageNumber, pageSize, container) {
    let params = getPaginationHeaders(pageNumber, pageSize);
    params = params.append('Container', container);
    return getPaginatedResult<Message[]>(this.url, params, this.http);
  }

  getMessageThread(username: string) {
    return this.http.get<Message[]>(this.url + 'thread/' + username);
  }

  async sendMessage(username: string, content: string) {
    return this.hubConnection.invoke('SendMessage', { recipientUsername: username, content })
      .catch(error => console.log(error));
  }

  deleteMessage(id: number) {
    return this.http.delete(this.url + id);
  }
}
