import { ComponentFixture, TestBed } from "@angular/core/testing";
import { RouterLink } from "@angular/router";

import { RegistroPaciente } from "./registro-paciente";
import { FormsModule } from "@angular/forms";

describe("RegistroPaciente", () => {
  let component: RegistroPaciente;
  let fixture: ComponentFixture<RegistroPaciente>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegistroPaciente, FormsModule, RouterLink],
    }).compileComponents();

    fixture = TestBed.createComponent(RegistroPaciente);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
