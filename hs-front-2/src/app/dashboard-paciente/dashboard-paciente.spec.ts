import { ComponentFixture, TestBed } from "@angular/core/testing";

import { DashboardPaciente } from "./dashboard-paciente";

describe("DashboardPaciente", () => {
  let component: DashboardPaciente;
  let fixture: ComponentFixture<DashboardPaciente>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPaciente],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardPaciente);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
