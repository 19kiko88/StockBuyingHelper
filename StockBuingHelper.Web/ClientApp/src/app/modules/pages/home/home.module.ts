import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MainComponent } from './components/main/main.component';
import { TestPageComponent } from './components/test-page/test-page.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [
    MainComponent,
    TestPageComponent,    
  ],
  imports: [
    CommonModule,    
    FormsModule, 
    ReactiveFormsModule,
    SharedModule
  ],
  exports:[
    MainComponent,
    TestPageComponent
  ]
})
export class HomeModule { }
