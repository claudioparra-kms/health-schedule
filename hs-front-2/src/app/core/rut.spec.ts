import { normalizarRut, validarRut } from './rut';

describe('Utilidades de RUT', () => {
  describe('normalizarRut', () => {
    it('elimina puntos y mantiene el guion antes del digito verificador', () => {
      expect(normalizarRut('12.345.678-5')).toBe('12345678-5');
    });

    it('convierte la letra k a mayuscula', () => {
      expect(normalizarRut('12.345.670-k')).toBe('12345670-K');
    });

    it('elimina espacios y caracteres ajenos al RUT', () => {
      expect(normalizarRut(' 19 264 452 / 6 ')).toBe('19264452-6');
    });
  });

  describe('validarRut', () => {
    it('acepta un RUT valido con digito numerico', () => {
      expect(validarRut('12345678-5')).toBe(true);
    });

    it('acepta un RUT valido con digito K', () => {
      expect(validarRut('12345670-k')).toBe(true);
    });

    it('acepta el RUT aun cuando contiene puntos', () => {
      expect(validarRut('19.264.452-6')).toBe(true);
    });

    it('rechaza un digito verificador incorrecto', () => {
      expect(validarRut('12345678-9')).toBe(false);
    });

    it('rechaza texto que no corresponde a un RUT', () => {
      expect(validarRut('rut-invalido')).toBe(false);
    });

    it('rechaza un RUT demasiado corto', () => {
      expect(validarRut('1234-3')).toBe(false);
    });
  });
});
