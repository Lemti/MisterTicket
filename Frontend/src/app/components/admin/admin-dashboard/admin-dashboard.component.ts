import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { SignalrService } from '../../../services/signalr.service';
import { Subscription } from 'rxjs';

interface Reservation {
  id: number;
  userName: string;
  eventName: string;
  totalAmount: number;
  status: string;
  reservationDate: string;
  seatNumbers: string[];
}

interface Stats {
  totalReservations: number;
  totalRevenue: number;
  pendingReservations: number;
  paidReservations: number;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  reservations: Reservation[] = [];
  stats: Stats = {
    totalReservations: 0,
    totalRevenue: 0,
    pendingReservations: 0,
    paidReservations: 0
  };
  loading = true;
  private subscriptions: Subscription[] = [];

  constructor(
    private http: HttpClient,
    private signalrService: SignalrService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    this.loadReservations();
    
    // Subscribe aux événements temps réel (seulement côté browser)
    if (isPlatformBrowser(this.platformId)) {
      this.subscriptions.push(
        this.signalrService.seatReserved$.subscribe(() => {
          console.log('Nouvelle réservation détectée');
          this.loadReservations();
        })
      );

      this.subscriptions.push(
        this.signalrService.paymentCompleted$.subscribe(() => {
          console.log('Paiement complété');
          this.loadReservations();
        })
      );

      this.subscriptions.push(
        this.signalrService.seatReleased$.subscribe(() => {
          console.log('Réservation annulée');
          this.loadReservations();
        })
      );
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  loadReservations(): void {
    this.http.get<Reservation[]>('https://localhost:7255/api/Reservations').subscribe({
      next: (data) => {
        this.reservations = data.sort((a, b) => 
          new Date(b.reservationDate).getTime() - new Date(a.reservationDate).getTime()
        );
        this.calculateStats();
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
      }
    });
  }

  calculateStats(): void {
    this.stats.totalReservations = this.reservations.length;
    this.stats.totalRevenue = this.reservations
      .filter(r => r.status === 'Paid')
      .reduce((sum, r) => sum + r.totalAmount, 0);
    this.stats.pendingReservations = this.reservations.filter(r => r.status === 'Pending').length;
    this.stats.paidReservations = this.reservations.filter(r => r.status === 'Paid').length;
  }

  getStatusBadge(status: string): string {
    switch(status) {
      case 'Paid': return 'badge bg-success';
      case 'Pending': return 'badge bg-warning text-dark';
      case 'Cancelled': return 'badge bg-danger';
      default: return 'badge bg-secondary';
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', { 
      day: 'numeric',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}