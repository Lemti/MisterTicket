import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../services/auth.service';

interface Event {
  id: number;
  name: string;
  description: string;
  eventDate: string;
  category: string;
  round: string;
  courtId: number;
  courtName: string;
}

interface Court {
  id: number;
  name: string;
}

@Component({
  selector: 'app-admin-events',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-events.component.html',
  styleUrls: ['./admin-events.component.css']
})
export class AdminEventsComponent implements OnInit {
  events: Event[] = [];
  courts: Court[] = [];
  loading = true;
  showForm = false;
  editMode = false;
  
  eventForm = {
    id: 0,
    name: '',
    description: '',
    eventDate: '',
    category: '',
    round: '',
    courtId: 0
  };

  constructor(
    private http: HttpClient,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadCourts();
    this.loadEvents();
  }

  loadCourts(): void {
    this.http.get<Court[]>('https://localhost:7255/api/Courts').subscribe({
      next: (data) => this.courts = data,
      error: (err) => console.error(err)
    });
  }

  loadEvents(): void {
    // Si Organizer, charger seulement SES événements
    const url = this.isOrganizer() 
      ? 'https://localhost:7255/api/Events/my' 
      : 'https://localhost:7255/api/Events';

    this.http.get<Event[]>(url).subscribe({
      next: (data) => {
        this.events = data;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
      }
    });
  }

  isOrganizer(): boolean {
    const user = this.authService.getCurrentUser();
    return user?.role === 'Organizer';
  }

  openCreateForm(): void {
    this.editMode = false;
    this.eventForm = {
      id: 0,
      name: '',
      description: '',
      eventDate: '',
      category: '',
      round: '',
      courtId: this.courts.length > 0 ? this.courts[0].id : 0
    };
    this.showForm = true;
  }

  openEditForm(event: Event): void {
    this.editMode = true;
    // Convertir la date ISO en format datetime-local
    const date = new Date(event.eventDate);
    const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);

    this.eventForm = {
      id: event.id,
      name: event.name,
      description: event.description,
      eventDate: localDate,
      category: event.category,
      round: event.round,
      courtId: event.courtId
    };
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
  }

  saveEvent(): void {
    if (this.editMode) {
      // UPDATE
      this.http.put(`https://localhost:7255/api/Events/${this.eventForm.id}`, this.eventForm).subscribe({
        next: () => {
          alert('Événement modifié avec succès');
          this.loadEvents();
          this.closeForm();
        },
        error: (err) => alert('Erreur: ' + (err.error?.message || err.message))
      });
    } else {
      // CREATE
      this.http.post('https://localhost:7255/api/Events', this.eventForm).subscribe({
        next: () => {
          alert('Événement créé avec succès');
          this.loadEvents();
          this.closeForm();
        },
        error: (err) => alert('Erreur: ' + (err.error?.message || err.message))
      });
    }
  }

  deleteEvent(id: number, name: string): void {
    if (!confirm(`Voulez-vous vraiment supprimer l'événement "${name}" ?`)) return;
    
    this.http.delete(`https://localhost:7255/api/Events/${id}`).subscribe({
      next: () => {
        alert('Événement supprimé avec succès');
        this.loadEvents();
      },
      error: (err) => alert('Erreur: ' + (err.error?.message || err.message))
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', { 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}