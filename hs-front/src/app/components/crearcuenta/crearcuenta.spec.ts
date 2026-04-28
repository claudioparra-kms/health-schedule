import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Crearcuenta } from './crearcuenta';

describe('Crearcuenta', () => {
  let component: Crearcuenta;
  let fixture: ComponentFixture<Crearcuenta>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Crearcuenta],
    }).compileComponents();

    fixture = TestBed.createComponent(Crearcuenta);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
