import os
import json
from jinja2 import Environment, FileSystemLoader


def create_chart_html(chart_data, output_path):
    # Load the Jinja2 template
    env = Environment(loader=FileSystemLoader('.'))
    template = env.get_template('chart_template.html')

    # Render the HTML with the chart data
    html_content = template.render(chart_data=chart_data)

    # Write the rendered HTML to a file
    with open(output_path, 'w') as f:
        f.write(html_content)


if __name__ == "__main__":
    training_history_path = os.path.join(
        "SavedSpecimen", "training_history.json")
    with open(training_history_path, "r") as f:
        # [[1,2,3,4,5,6,7,8,9,10], [1,2,3,4,5,6,7,8,9,10]]
        training_history_data = json.load(f)
        chart_data = {"fitness_data": training_history_data, "labels": list(
            range(len(training_history_data[0])))}
    create_chart_html(chart_data, "fitness_chart.html")
