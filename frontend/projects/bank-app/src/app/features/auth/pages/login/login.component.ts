import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
// Atomları import ediyoruz
import { InputComponent } from '../../../../../../../ui/src/lib/atoms/input/input.component';
import { ButtonComponent } from '../../../../../../../ui/src/lib/atoms/button/button.component';

@Component({
  selector: 'app-login',
  standalone: true,
  // Buraya eklediğinde hata gidecek
  imports: [CommonModule, RouterModule, InputComponent, ButtonComponent], 
  templateUrl: './login.component.html',
  styles: [] // scss hatası almamak için şimdilik böyle kalsın
})
export class LoginComponent {}
// styleUrls satırını siliyoruz veya boş bırakıyoruz