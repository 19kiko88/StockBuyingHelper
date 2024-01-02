import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingComponent } from './components/loading/loading.component';
import { PrimeNgModule } from './prime-ng/prime-ng.module';
import { RatingModule } from 'primeng/rating';
import { ButtonModule } from 'primeng/button';



@NgModule({
  declarations: [
    LoadingComponent
  ],
  imports: [
    CommonModule,
    PrimeNgModule,
    RatingModule,
    ButtonModule
  ],
  exports:[
    LoadingComponent,
    PrimeNgModule,
    RatingModule,
    ButtonModule
  ]
})
export class SharedModule { }
