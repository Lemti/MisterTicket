import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminCourtsComponent } from './admin-courts.component';

describe('AdminCourtsComponent', () => {
  let component: AdminCourtsComponent;
  let fixture: ComponentFixture<AdminCourtsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminCourtsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AdminCourtsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
