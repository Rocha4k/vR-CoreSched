import * as signalR from '@microsoft/signalr';
import { API_URL } from './api';

export function createOperationsConnection(token: string) {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${API_URL}/hubs/operations`, {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();
}
