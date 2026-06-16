import { ComponentFixture, TestBed } from "@angular/core/testing";

import { DashboardDoctor } from "./dashboard-doctor";

describe("DashboardDoctor", () => {
  let component: DashboardDoctor;
  let fixture: ComponentFixture<DashboardDoctor>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardDoctor],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardDoctor);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
