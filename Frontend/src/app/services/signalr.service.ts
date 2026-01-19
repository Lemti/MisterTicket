import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection?: signalR.HubConnection;
  
  // Observables pour les événements temps réel
  public seatReserved$ = new Subject<any>();
  public seatReleased$ = new Subject<any>();
  public paymentCompleted$ = new Subject<any>();

  constructor() { }

  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7255/hubs/reservation')
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connected!');
        this.registerEvents();
      })
      .catch(err => console.error('Error while starting SignalR connection: ' + err));
  }

  private registerEvents(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('SeatReserved', (data) => {
      console.log('SeatReserved:', data);
      this.seatReserved$.next(data);
    });

    this.hubConnection.on('SeatReleased', (data) => {
      console.log('SeatReleased:', data);
      this.seatReleased$.next(data);
    });

    this.hubConnection.on('PaymentCompleted', (data) => {
      console.log('PaymentCompleted:', data);
      this.paymentCompleted$.next(data);
    });
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }
}