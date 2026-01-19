import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface Court {
  id: number;
  name: string;
  capacity: number;
  description: string;
}

@Component({
  selector: 'app-admin-courts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-courts.component.html',
  styleUrls: ['./admin-courts.component.css']
})
export class AdminCourtsComponent implements OnInit {
  courts: Court[] = [];
  loading = true;
  showForm = false;
  editMode = false;

  courtForm = {
    id: 0,
    name: '',
    capacity: 0,
    description: ''
  };
  showGeneratorForm = false;
  generatorForm = {
    courtId: 0,
    courtName: '',
    template: 'medium',
    vipPrice: 500,
    tribunePrice: 250,
    gradinPrice: 100
  };

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    this.loadCourts();
  }

  loadCourts(): void {
    this.http.get<Court[]>('https://localhost:7255/api/Courts').subscribe({
      next: (data) => {
        this.courts = data;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
      }
    });
  }

  openCreateForm(): void {
    this.editMode = false;
    this.courtForm = { id: 0, name: '', capacity: 0, description: '' };
    this.showForm = true;
  }

  openEditForm(court: Court): void {
    this.editMode = true;
    this.courtForm = { ...court };
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.courtForm = { id: 0, name: '', capacity: 0, description: '' };
  }

  saveCourt(): void {
    if (this.editMode) {
      // UPDATE
      this.http.put(`https://localhost:7255/api/Courts/${this.courtForm.id}`, this.courtForm).subscribe({
        next: () => {
          alert('Court modifié avec succès');
          this.loadCourts();
          this.closeForm();
        },
        error: (err) => alert('Erreur: ' + (err.error?.message || err.message))
      });
    } else {
      // CREATE
      this.http.post('https://localhost:7255/api/Courts', this.courtForm).subscribe({
        next: () => {
          alert('Court créé avec succès');
          this.loadCourts();
          this.closeForm();
        },
        error: (err) => alert('Erreur: ' + (err.error?.message || err.message))
      });
    }
  }

  deleteCourt(id: number, name: string): void {
    if (!confirm(`Voulez-vous vraiment supprimer le court "${name}" ?`)) return;

    this.http.delete(`https://localhost:7255/api/Courts/${id}`).subscribe({
      next: () => {
        alert('Court supprimé avec succès');
        this.loadCourts();
      },
      error: (err) => alert('Erreur: ' + (err.error?.message || err.message))
    });
  }
  openGeneratorForm(court: any): void {
    this.generatorForm = {
      courtId: court.id,
      courtName: court.name,
      template: 'medium',
      vipPrice: 500,
      tribunePrice: 250,
      gradinPrice: 100
    };
    this.showGeneratorForm = true;
  }

  closeGeneratorForm(): void {
    this.showGeneratorForm = false;
  }

  generateSeats(): void {
    if (!confirm(`Cela va supprimer tous les sièges existants du court "${this.generatorForm.courtName}" et en générer de nouveaux. Continuer ?`)) {
      return;
    }

    const payload = {
      template: this.generatorForm.template,
      vipPrice: this.generatorForm.vipPrice,
      tribunePrice: this.generatorForm.tribunePrice,
      gradinPrice: this.generatorForm.gradinPrice
    };

    this.http.post(`https://localhost:7255/api/Seats/generate/${this.generatorForm.courtId}`, payload).subscribe({
      next: (response: any) => {
        alert(`✅ ${response.count} sièges générés avec succès !`);
        this.closeGeneratorForm();
      },
      error: (err) => alert('Erreur: ' + (err.error?.message || err.message))
    });
  }

  getTemplateDescription(template: string): string {
    switch (template) {
      case 'small': return 'Petit court (~40 places) : Tribune + Gradin';
      case 'medium': return 'Moyen court (~70 places) : VIP + Tribune + Gradin';
      case 'large': return 'Grand court (~120 places) : VIP Premium + Tribunes + Gradin';
      default: return '';
    }
  }
}