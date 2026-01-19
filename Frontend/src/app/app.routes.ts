import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home.component';
import { EventListComponent } from './components/event-list/event-list.component';
import { EventDetailComponent } from './components/event-detail/event-detail.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { MyReservationsComponent } from './components/my-reservations/my-reservations.component';
import { AdminCourtsComponent } from './components/admin/admin-courts/admin-courts.component';
import { AdminEventsComponent } from './components/admin/admin-events/admin-events.component';
import { AdminDashboardComponent } from './components/admin/admin-dashboard/admin-dashboard.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'events', component: EventListComponent },
  { path: 'events/:id', component: EventDetailComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'my-reservations', component: MyReservationsComponent },
  { path: 'admin/courts', component: AdminCourtsComponent },
  { path: 'admin/events', component: AdminEventsComponent },
  { path: 'admin/dashboard', component: AdminDashboardComponent },
  { path: '**', redirectTo: '' }
];