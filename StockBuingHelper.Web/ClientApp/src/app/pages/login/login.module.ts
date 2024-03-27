import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LoginComponent } from './components/login/login.component';
import { SharedModule } from 'primeng/api';



@NgModule({
  declarations: [LoginComponent],
  imports: [
    CommonModule,    
    FormsModule, 
    ReactiveFormsModule,
    SharedModule
  ],
  exports:[LoginComponent]
})
export class LoginModule { }
