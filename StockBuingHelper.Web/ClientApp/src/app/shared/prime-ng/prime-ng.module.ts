import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { SliderModule } from 'primeng/slider';


@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    TableModule,
    SliderModule
  ],
  exports:[    
    TableModule,
    SliderModule
  ]
})
export class PrimeNgModule { }
