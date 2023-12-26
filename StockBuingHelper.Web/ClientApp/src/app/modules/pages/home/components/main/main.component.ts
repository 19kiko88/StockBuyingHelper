import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';


@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})

export class MainComponent implements OnInit 
{

  form!:FormGroup;

  constructor(
    private _formBuilder: FormBuilder,
  ) { }

  ngOnInit(): void {
    this.form = this._formBuilder.group({
      ggg : ['99999'],
    })
  }

  submit():void
  {
    window.alert('11233');
  }
}
