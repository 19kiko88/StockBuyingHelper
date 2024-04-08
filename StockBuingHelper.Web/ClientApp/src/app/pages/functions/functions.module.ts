import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '../../shared/shared.module';
import { VtiQueryComponent } from './components/vti-query/vti-query.component';

@NgModule({
  declarations: [VtiQueryComponent],
  imports: [
    CommonModule,    
    FormsModule, 
    ReactiveFormsModule,
    SharedModule
  ],
  exports:[VtiQueryComponent]
})
export class FunctionsModule { }
