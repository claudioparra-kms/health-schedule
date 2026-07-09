export function normalizarRut(valor: string): string {
  const limpio = valor.toUpperCase().replace(/[^0-9K]/g, '');
  if (limpio.length < 2) return limpio;
  return `${limpio.slice(0, -1)}-${limpio.slice(-1)}`;
}

export function validarRut(valor: string): boolean {
  const rut = normalizarRut(valor);
  const [cuerpoTexto, dv] = rut.split('-');
  if (!cuerpoTexto || !dv || !/^\d{7,8}$/.test(cuerpoTexto)) return false;

  let suma = 0;
  let multiplicador = 2;
  for (let i = cuerpoTexto.length - 1; i >= 0; i -= 1) {
    suma += Number(cuerpoTexto[i]) * multiplicador;
    multiplicador = multiplicador === 7 ? 2 : multiplicador + 1;
  }

  const resultado = 11 - (suma % 11);
  const esperado = resultado === 11 ? '0' : resultado === 10 ? 'K' : String(resultado);
  return dv === esperado;
}
