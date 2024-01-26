import { HomeModule } from './home/home.module';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminModule } from './admin/admin.module';


@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    HomeModule,
    AdminModule
  ],
  exports:[
    HomeModule,
    AdminModule
  ]
})
export class PagesModule { }
