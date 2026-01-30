import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ButtonComponent } from '../../../../../../../ui/src/lib/atoms/button/button.component';
import { InputComponent } from '../../../../../../../ui/src/lib/atoms/input/input.component';
import { AuthApi } from '../../../../../../../data-access/src/lib/api/auth.api';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, InputComponent, ButtonComponent],
  templateUrl: './register.component.html',
  styles: []
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApi);
  private router = inject(Router);

  registerForm = this.fb.group({
    tcNo: ['', [Validators.required, Validators.minLength(11)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
    password: ['', Validators.required],
    membership: ['Personal']
  });

  onSubmit() {
    if (this.registerForm.valid) {
      const api = this.authApi as any; 
      
      api.register(this.registerForm.value).subscribe({
        next: (response: any) => {
          console.log('Kayıt Başarılı!', response);
          this.router.navigate(['/auth/login']);
        },
        error: (err: any) => { 
          console.error('Kayıt Hatası:', err);
          alert('Kayıt sırasında bir sorun oluştu.');
        }
      });
    }
  }
}