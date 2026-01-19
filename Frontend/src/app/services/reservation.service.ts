import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface ReservationDto {
  id: number;
  reservationDate: string;
  status: string; // 'Pending', 'Paid', 'Cancelled'
  totalAmount: number;
  expiresAt: string | null;
  userId: number;
  userName: string;
  eventId: number;
  eventName: string;
  eventDate?: string;
  seatIds: number[];
  seatNumbers: string[];
}

export interface CreateReservationDto {
  eventId: number;
  seatIds: number[];
}

export interface CreatePaymentDto {
  reservationId: number;
  paymentMethod: string;
}

export interface CancelResponse {
  message: string;
  reservationId: number;
  refundAmount?: number;
  seatsFreed?: number;
  cancelledBy?: string;
}

@Injectable({ providedIn: 'root' })
export class ReservationService {
  private apiUrl = 'https://localhost:7255/api/Reservations';
  private paymentUrl = 'https://localhost:7255/api/Payments';

  constructor(private http: HttpClient) {}

  createReservation(data: CreateReservationDto): Observable<ReservationDto> {
    return this.http.post<ReservationDto>(this.apiUrl, data).pipe(catchError(this.handleError));
  }

  getReservation(id: number): Observable<ReservationDto> {
    return this.http.get<ReservationDto>(`${this.apiUrl}/${id}`).pipe(catchError(this.handleError));
  }

  getMyReservations(): Observable<ReservationDto[]> {
    return this.http.get<ReservationDto[]>(`${this.apiUrl}/my`).pipe(catchError(this.handleError));
  }

  getAllReservations(): Observable<ReservationDto[]> {
    return this.http.get<ReservationDto[]>(this.apiUrl).pipe(catchError(this.handleError));
  }

  cancelReservation(id: number): Observable<CancelResponse> {
    return this.http.put<CancelResponse>(`${this.apiUrl}/${id}/cancel`, {}).pipe(catchError(this.handleError));
  }

  cancelReservationAsAdmin(id: number, reason: string): Observable<CancelResponse> {
    return this.http.put<CancelResponse>(`${this.apiUrl}/${id}/admin-cancel`, { reason }).pipe(catchError(this.handleError));
  }

  processPayment(data: CreatePaymentDto): Observable<any> {
    return this.http.post(this.paymentUrl, data).pipe(catchError(this.handleError));
  }

  downloadTicket(reservationId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${reservationId}/ticket`, { responseType: 'blob' })
      .pipe(catchError(this.handleError));
  }

  // ✅ CORRIGÉ : Gestion d'erreur sans ErrorEvent
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'Une erreur est survenue';

    // ✅ Vérifie si c'est une HttpErrorResponse (erreur backend)
    if (error.error && typeof error.error === 'object') {
      // Backend a renvoyé un objet avec message
      const backendMessage = error.error.message || error.error.details;
      if (backendMessage) {
        errorMessage = backendMessage;
      }
    } else if (typeof error.error === 'string') {
      // Backend a renvoyé une string
      errorMessage = error.error;
    }

    // Messages personnalisés selon le code HTTP
    switch (error.status) {
      case 0:
        errorMessage = 'Impossible de contacter le serveur. Vérifiez votre connexion.';
        break;
      case 400:
        errorMessage = error.error?.message || 'Requête invalide (400)';
        break;
      case 401:
        errorMessage = 'Non autorisé. Veuillez vous reconnecter.';
        break;
      case 403:
        errorMessage = 'Accès interdit (403)';
        break;
      case 404:
        errorMessage = error.error?.message || 'Ressource non trouvée (404)';
        break;
      case 409:
        errorMessage = error.error?.message || 'Conflit de données (409)';
        break;
      case 500:
        errorMessage = error.error?.message || 'Erreur serveur (500)';
        break;
      default:
        if (!errorMessage || errorMessage === 'Une erreur est survenue') {
          errorMessage = `Erreur ${error.status}: ${error.message || 'Erreur inconnue'}`;
        }
    }

    console.error('❌ ReservationService Error:', {
      status: error.status,
      message: errorMessage,
      fullError: error
    });

    return throwError(() => new Error(errorMessage));
  }
}