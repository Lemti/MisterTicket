import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReservationService, ReservationDto } from '../../services/reservation.service';

@Component({
  selector: 'app-my-reservations',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './my-reservations.component.html',
  styleUrls: ['./my-reservations.component.css']
})
export class MyReservationsComponent implements OnInit {
  reservations: ReservationDto[] = [];
  loading = true;
  errorMessage: string | null = null;
  processing = false;  // ✅ Protection double-clic pour le paiement

  constructor(private reservationService: ReservationService) { }

  ngOnInit(): void {
    this.loadReservations();
    // ✅ Auto-refresh toutes les 30 secondes pour mettre à jour les timers
    setInterval(() => {
      if (!this.loading && !this.processing) {
        this.loadReservations();
      }
    }, 30000);
  }

  loadReservations(): void {
    this.loading = true;
    this.errorMessage = null;
    
    this.reservationService.getMyReservations().subscribe({
      next: (data) => {
        this.reservations = data;
        
        // ✅ DEBUG : Affiche les statuts dans la console
        console.log('📋 Réservations chargées:', data);
        data.forEach(r => {
          console.log(`#${r.id} - Status: "${r.status}" - Montant: ${r.totalAmount}€`);
        });
        
        this.loading = false;
      },
      error: (err) => {
        console.error('Erreur chargement réservations:', err);
        this.errorMessage = err.message || 'Impossible de charger vos réservations';
        this.loading = false;
      }
    });
  }

  // ✅ PAIEMENT DIRECT
  payNow(reservation: ReservationDto): void {
    if (this.processing) {
      console.log('⚠️ Paiement déjà en cours');
      return;
    }

    const confirmMsg = `Confirmer le paiement de ${reservation.totalAmount}€ pour :\n\n` +
                       `📅 ${reservation.eventName}\n` +
                       `🪑 Places: ${reservation.seatNumbers.join(', ')}\n\n` +
                       `💳 (Paiement simulé - Formation)`;

    if (!confirm(confirmMsg)) return;

    this.processing = true;

    this.reservationService.processPayment({
      reservationId: reservation.id,
      paymentMethod: 'SIMULATION'
    }).subscribe({
      next: () => {
        alert('✅ Paiement effectué avec succès !\n\n' +
              '🎫 Vous pouvez maintenant télécharger votre billet PDF.\n' +
              '📧 Un email de confirmation a été envoyé (simulation).');
        this.processing = false;
        this.loadReservations(); // Recharge pour mettre à jour le statut
      },
      error: (err) => {
        alert('❌ Erreur de paiement:\n\n' + err.message);
        this.processing = false;
      }
    });
  }

  // Vérifier si on peut annuler
  canCancel(reservation: ReservationDto): boolean {
    // On peut annuler si :
    // 1. Pas déjà annulée
    // 2. Statut Paid ou Pending
    // 3. L'événement n'est pas passé (si on a la date)
    
    if (reservation.status === 'Cancelled') return false;
    
    const isValidStatus = reservation.status === 'Paid' || reservation.status === 'Pending';
    
    // Vérifier si l'événement est passé
    if (reservation.eventDate) {
      const eventDate = new Date(reservation.eventDate);
      const now = new Date();
      if (eventDate < now) return false; // Événement déjà passé
    }
    
    return isValidStatus;
  }

  getStatusBadge(status: string): string {
    switch (status) {
      case 'Paid': return 'badge bg-success';
      case 'Pending': return 'badge bg-warning text-dark';
      case 'Cancelled': return 'badge bg-secondary';
      default: return 'badge bg-light text-dark';
    }
  }

  getStatusText(status: string): string {
    switch (status) {
      case 'Paid': return '✅ Payé';
      case 'Pending': return '⏳ En attente de paiement';
      case 'Cancelled': return '❌ Annulé';
      default: return status;
    }
  }

  // Bouton Annuler avec style différent selon statut
  getCancelButtonClass(status: string): string {
    switch (status) {
      case 'Paid': return 'btn btn-warning';
      case 'Pending': return 'btn btn-danger';
      default: return 'btn btn-secondary';
    }
  }

  getCancelButtonText(status: string): string {
    switch (status) {
      case 'Paid': return '❌ Annuler (remboursement)';
      case 'Pending': return '❌ Annuler la réservation';
      default: return 'Annuler';
    }
  }

  // Annuler une réservation
  cancelReservation(reservationId: number): void {
    const reservation = this.reservations.find(r => r.id === reservationId);
    
    if (!reservation) return;
    
    // Message de confirmation personnalisé
    let confirmMessage = 'Voulez-vous vraiment annuler cette réservation ?\n\n';
    
    if (reservation.status === 'Paid') {
      confirmMessage += '💰 Un remboursement fictif sera effectué.\n';
      confirmMessage += `Montant à rembourser: ${reservation.totalAmount}€\n\n`;
    }
    
    confirmMessage += '🪑 Les sièges seront immédiatement libérés.';
    
    if (!confirm(confirmMessage)) return;

    this.reservationService.cancelReservation(reservationId).subscribe({
      next: (response) => {
        alert(response.message || 'Réservation annulée avec succès');
        this.loadReservations(); // Recharger
      },
      error: (err) => {
        alert('Erreur: ' + err.message);
      }
    });
  }

  // Télécharger le ticket PDF
  downloadTicket(reservationId: number): void {
    this.reservationService.downloadTicket(reservationId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Billet-Reservation-${reservationId}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        alert('Erreur lors du téléchargement: ' + err.message);
      }
    });
  }

  // Formater la date
  formatDate(dateString: string): string {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('fr-FR', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return dateString;
    }
  }

  // Temps restant pour les réservations en attente
  getTimeRemaining(expiresAt: string | null): string {
    if (!expiresAt) return '';
    
    try {
      const now = new Date().getTime();
      const expiry = new Date(expiresAt).getTime();
      const diff = expiry - now;

      if (diff <= 0) return '⏰ Expiré';

      const minutes = Math.floor(diff / 60000);
      const hours = Math.floor(minutes / 60);
      const days = Math.floor(hours / 24);
      
      if (days > 0) {
        return `⏳ ${days}j ${hours % 24}h restantes`;
      } else if (hours > 0) {
        return `⏳ ${hours}h ${minutes % 60}min restantes`;
      } else {
        return `⏳ ${minutes} min restantes`;
      }
    } catch {
      return '';
    }
  }
}