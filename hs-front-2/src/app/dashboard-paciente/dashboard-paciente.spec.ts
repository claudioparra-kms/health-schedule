import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardPaciente } from './dashboard-paciente';

describe('DashboardPaciente', () => {
  let component: DashboardPaciente;
  let fixture: ComponentFixture<DashboardPaciente>;
  let compiled: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPaciente]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardPaciente);
    component = fixture.componentInstance;
    compiled = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
import { HttpClient } from '@angular/common/http';
import { HttpClientTestingModule } from '@angular/common/http/testing';

