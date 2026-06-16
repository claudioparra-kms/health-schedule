import { ComponentFixture, TestBed } from "@angular/core/testing";

import { DashboardInvitado } from "./dashboard-invitado";

describe("DashboardInvitado", () => {
  let component: DashboardInvitado;
  let fixture: ComponentFixture<DashboardInvitado>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardInvitado],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardInvitado);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
