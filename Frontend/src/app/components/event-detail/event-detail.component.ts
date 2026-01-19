import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';

import { SeatMapComponent, Seat } from '../seat-map/seat-map.component';
import { ReservationService, ReservationDto } from '../../services/reservation.service';

interface EventDto {
  id: number;
  name: string;
  description: string;
  eventDate: string;
  category: string;
  round: string;
  courtId: number;
  courtName: string;
}

type Step = 'idle' | 'selected' | 'reserved' | 'paid';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, SeatMapComponent],
  templateUrl: './event-detail.component.html',
  styleUrls: ['./event-detail.component.css'],
})
export class EventDetailComponent implements OnInit {
  event: EventDto | null = null;
  seats: Seat[] = [];
  loading = true;

  selectedSeats: Seat[] = [];

  step: Step = 'idle';
  reserving = false;
  paying = false;
  downloading = false;

  isReserving = false;  // Protection double-clic


  reservation: ReservationDto | null = null;

  errorMsg: string | null = null;
  successMsg: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private reservationService: ReservationService
  ) { }

  ngOnInit(): void {
    const eventId = Number(this.route.snapshot.params['id']) || 0;
    this.loadEvent(eventId);
  }

  private loadEvent(eventId: number): void {
    this.http.get<EventDto>(`https://localhost:7255/api/Events/${eventId}`).subscribe({
      next: (ev) => {
        this.event = ev;
        this.loadSeats(ev.courtId);
      },
      error: () => {
        this.loading = false;
        this.event = null;
      }
    });
  }

  private loadSeats(courtId: number): void {
    this.http.get<Seat[]>(`https://localhost:7255/api/Seats/court/${courtId}`).subscribe({
      next: (seats) => {
        this.seats = seats;
        this.loading = false;
      },
      error: () => {
        this.seats = [];
        this.loading = false;
      }
    });
  }

  onSeatsSelected(seats: Seat[]): void {
    this.selectedSeats = seats;
    this.step = seats.length ? 'selected' : 'idle';
    this.errorMsg = null;
    this.successMsg = null;

    // reset reservation if user changes selection
    this.reservation = null;
  }

  get totalAmount(): number {
    return this.selectedSeats.reduce((sum, s) => sum + (s.price || 0), 0);
  }

commander(): void {
  if (!this.event || !this.selectedSeats.length) return;

  // 🛡️ PROTECTION DOUBLE-CLIC
  if (this.isReserving) {
    console.log('⚠️ Réservation déjà en cours, blocage du double-clic');
    return;
  }

  this.errorMsg = null;
  this.successMsg = null;
  this.isReserving = true;  // ✅ ACTIVE LE VERROU
  this.reserving = true;

  // ✅ LOG BRUT AVANT PARSING
  this.http.post(`https://localhost:7255/api/Reservations`, {
    eventId: this.event.id,
    seatIds: this.selectedSeats.map(s => s.id),
  }, {
    observe: 'response',
    responseType: 'text' as 'json' // Force texte pour voir ce qui arrive
  }).subscribe({
    next: (response: any) => {
      console.log('🔍 STATUS:', response.status);
      console.log('🔍 HEADERS:', response.headers);
      console.log('🔍 BODY BRUT:', response.body);
      console.log('🔍 TYPE:', typeof response.body);

      // Essaie de parser manuellement
      try {
        const parsed = JSON.parse(response.body);
        console.log('✅ PARSED OK:', parsed);
        this.reservation = parsed;
        this.step = 'reserved';
      } catch (e) {
        console.error('❌ PARSE FAILED:', e);
        this.errorMsg = 'Erreur de parsing: ' + response.body;
      } finally {
        // ✅ DÉSACTIVE TOUJOURS LE VERROU (succès ou erreur)
        this.reserving = false;
        this.isReserving = false;
      }
    },
    error: (e) => {
      console.error('❌ HTTP ERROR:', e);
      this.errorMsg = e.message;
      
      // ✅ DÉSACTIVE LE VERROU EN CAS D'ERREUR
      this.reserving = false;
      this.isReserving = false;
    }
  });
}

  payerSimulation(): void {
    if (!this.reservation) return;

    this.errorMsg = null;
    this.successMsg = null;
    this.paying = true;

    this.reservationService.processPayment({
      reservationId: this.reservation.id,
      paymentMethod: 'SIMULATION',
    }).subscribe({
      next: () => {
        // ✅ reload to get accurate status Paid
        this.reservationService.getMyReservations().subscribe({
          next: (list) => {
            const updated = list.find(r => r.id === this.reservation!.id) || this.reservation!;
            this.reservation = updated;
            this.step = 'paid';
            this.successMsg = 'Paiement simulé effectué ✅ — billet disponible';
            this.paying = false;

            if (this.event) this.loadSeats(this.event.courtId);

            // ✅ auto-open PDF (QR) immediately
            this.telechargerBillet();
          },
          error: () => {
            this.step = 'paid';
            this.successMsg = 'Paiement simulé effectué ✅ — billet disponible';
            this.paying = false;
          }
        });
      },
      error: (e: Error) => {
        this.errorMsg = e.message;
        this.paying = false;
      }
    });
  }

  telechargerBillet(): void {
    if (!this.reservation) return;

    this.downloading = true;
    this.errorMsg = null;

    this.reservationService.downloadTicket(this.reservation.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        this.downloading = false;
      },
      error: (e: Error) => {
        this.errorMsg = e.message;
        this.downloading = false;
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  goBack(): void {
    window.history.back();
  }
}
