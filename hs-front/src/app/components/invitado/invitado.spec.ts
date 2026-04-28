import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Invitado } from './invitado';

describe('Invitado', () => {
  let component: Invitado;
  let fixture: ComponentFixture<Invitado>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Invitado],
    }).compileComponents();

    fixture = TestBed.createComponent(Invitado);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
