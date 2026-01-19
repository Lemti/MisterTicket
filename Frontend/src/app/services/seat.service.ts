import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SeatDto {
  id: number;
  seatNumber: string;
  row: string;
  zone: string;
  price: number;
  status: string; // Available, Reserved, Paid
  courtId: number;
  courtName: string;
}

@Injectable({
  providedIn: 'root'
})
export class SeatService {
  private apiUrl = 'https://localhost:7255/api/Seats';

  constructor(private http: HttpClient) { }

  getSeats(): Observable<SeatDto[]> {
    return this.http.get<SeatDto[]>(this.apiUrl);
  }

  getSeatsByCourt(courtId: number): Observable<SeatDto[]> {
    return this.http.get<SeatDto[]>(`${this.apiUrl}/court/${courtId}`);
  }
}