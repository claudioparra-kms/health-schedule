import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({ selector: 'app-ficha-clinica',
            standalone: true,
            imports: [RouterLink],
            templateUrl: './ficha-clinica.html',
            styleUrls: ['./ficha-clinica.css'] })

export class FichaClinica {
    usuario: any = JSON.parse(localStorage.getItem('usuario') || '{}');
}
