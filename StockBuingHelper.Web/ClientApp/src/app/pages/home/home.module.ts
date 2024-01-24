import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MainComponent } from './components/main/main.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [
    MainComponent,    
  ],
  imports: [
    CommonModule,    
    FormsModule, 
    ReactiveFormsModule,
    SharedModule
  ],
  exports:[
    MainComponent
  ]
})
export class HomeModule { }
